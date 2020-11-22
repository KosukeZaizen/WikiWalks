using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using RelatedPages.Models;
using System.Linq;
using WikiWalks;
using System.Threading.Tasks;
using System.Text.Json;

namespace RelatedPages.Controllers
{
    [Route("api/[controller]")]
    public class WikiWalksController : Controller
    {
        private readonly AllWordsGetter allWorsGetter;
        private readonly AllCategoriesGetter allCategoriesGetter;

        public WikiWalksController(AllWordsGetter allWorsGetter, AllCategoriesGetter allCategoriesGetter)
        {
            this.allWorsGetter = allWorsGetter;
            this.allCategoriesGetter = allCategoriesGetter;
        }

        [HttpGet("[action]")]
        public IEnumerable<Page> getPartialWords(int num)
        {
            return allWorsGetter.getPages().Take(num);
        }

        [HttpGet("[action]")]
        public IEnumerable<object> getPartialCategories(int num)
        {
            return allCategoriesGetter.getCategories().Take(num);
        }

        [HttpGet("[action]")]
        public IEnumerable<Page> getWordsForCategory(string category)
        {
            DBCon con = new DBCon();
            List<Page> pages = new List<Page>();

            string sql = "select wordId from Category where category like @category;";

            var result = con.ExecuteSelect(sql, new Dictionary<string, object[]> { { "@category", new object[2] { SqlDbType.NVarChar, category } } });

            //急ぐ処理ではないので、Thread枯渇を考慮してTask.Runは使わない。
            result.ForEach(e =>
            {
                var page = allWorsGetter.getPages().FirstOrDefault(w => w.wordId == (int)e["wordId"]);
                if (page != null)
                {
                    pages.Add(page);
                }
            });

            return pages;
        }

        [HttpGet("[action]")]
        public IEnumerable<Page> getWordsForCategoryWithoutSnippet(string category, int top = 0)
        {
            var con = new DBCon();
            var pages = new List<Page>();

            string sql;
            List<Dictionary<string, object>> result;
            if (top == 0)
            {
                sql = "select wordId from Category where category like @category;";
                result = con.ExecuteSelect(sql, new Dictionary<string, object[]> { { "@category", new object[2] { SqlDbType.NVarChar, category } } });
            }
            else
            {
                sql = "select top(@top) wordId from Category where category like @category;";
                result = con.ExecuteSelect(sql, new Dictionary<string, object[]> {
                    { "@category", new object[2] { SqlDbType.NVarChar, category } },
                    { "@top", new object[2] { SqlDbType.Int, top } }
                });
            }

            result.ForEach(e =>
            {
                var page = allWorsGetter.getPages().FirstOrDefault(w => w.wordId == (int)e["wordId"]);
                if (page != null)
                {
                    //カテゴリページにはSnippetが必要ないため、通信量削減のため除去
                    pages.Add(new Page()
                    {
                        wordId = page.wordId,
                        word = page.word,
                        referenceCount = page.referenceCount
                    });
                }
            });

            return pages;
        }

        [HttpGet("[action]")]
        public object getWord(int wordId)
        {
            if (wordId <= 0) return "";

            var con = new DBCon();
            var result = con.ExecuteSelect("select top(1) word from Word where wordId = @wordId;", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } });
            return new { word = (string)result.FirstOrDefault()["word"] };
        }


        public class RelatedArticlesResponse
        {
            public IEnumerable<Page> pages;
        }
        [HttpGet("[action]")]
        public string getRelatedArticles(int wordId)
        {
            if (wordId <= 0) return "{}";

            var con = new DBCon();

            Func<string> getRelatedArticlesWithoutCache = () =>
            {
                var ps = new List<Page>();

                var result = con.ExecuteSelect(@"
select w.wordId, w.word, wr.snippet from Word as w
inner join
(select top(500) sourceWordId, snippet from WordReference where targetWordId = @wordId)
as wr
on w.wordId = wr.sourceWordId;
", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } });

                result.ForEach((e) =>
                {
                    var page = allWorsGetter.getPages().FirstOrDefault(w => w.wordId == (int)e["wordId"]);
                    if (page == null)
                    {
                        page = new Page();
                        page.wordId = (int)e["wordId"];
                        page.word = (string)e["word"];
                        page.referenceCount = 0;
                    }
                    page.snippet = (string)e["snippet"];
                    ps.Add(page);
                });

                if (ps.Any(p => p.referenceCount > 0))
                {
                    var pages = ps.OrderByDescending(p => p.referenceCount).ToList();
                    return JsonSerializer.Serialize(new { pages });
                }
                else
                {
                    //デプロイ直後でまだallWorsGetterの準備ができていない場合は、
                    //キャッシュテーブルに登録しない
                    return "{}";
                }
            };


            //キャッシュ取得
            var cache = con.ExecuteSelect(@"
select wordId, response
from RelatedArticlesCache
where wordId = @wordId
", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } }).FirstOrDefault();

            if (cache != null)
            {
                //キャッシュデータあり
                var cachedResponse = (string)cache["response"];

                Task.Run(async () =>
                {
                    //10秒待って再取得・更新
                    await Task.Delay(10 * 1000);
                    string json = getRelatedArticlesWithoutCache();

                    if (json.Contains("pages") && !json.Equals(cachedResponse))
                    {
                        //既にキャッシュされているものとの差分がある場合、キャッシュ内容をupdate
                        await Task.Delay(2 * 1000);
                        con.ExecuteUpdate(@"
update RelatedArticlesCache
set response = @json
where wordId = @wordId
", new Dictionary<string, object[]> {
                            { "@json", new object[2] { SqlDbType.NVarChar, json } },
                            { "@wordId", new object[2] { SqlDbType.Int, wordId } }
                        });
                    }
                });

                //上記完了を待たずに、キャッシュされていたデータを返す
                return cachedResponse;
            }
            else
            {
                //キャッシュデータなし
                string json = getRelatedArticlesWithoutCache();

                Task.Run(async () =>
                {
                    //2秒待って登録
                    await Task.Delay(2 * 1000);
                    if (json.Contains("pages"))
                    {
                        con.ExecuteUpdate("insert into RelatedArticlesCache values(@wordId, @json);", new Dictionary<string, object[]> {
                            { "@json", new object[2] { SqlDbType.NVarChar, json } },
                            { "@wordId", new object[2] { SqlDbType.Int, wordId } }
                        });
                    }
                });
                return json;
            }
        }

        [HttpGet("[action]")]
        public object getRelatedCategories(int wordId)
        {
            if (wordId <= 0) return new { };

            var con = new DBCon();
            var categories = new List<Category>();

            var result = con.ExecuteSelect("select category from Category where wordId = @wordId;", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } });
            result.ForEach((f) =>
            {
                var c = allCategoriesGetter.getCategories().FirstOrDefault(ca => ca.category == (string)f["category"]);
                if (c != null)
                {
                    categories.Add(c);
                }
            });

            return new { categories };
        }
    }
}

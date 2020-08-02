using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using RelatedPages.Models;
using System.Linq;
using WikiWalks;
using System.Threading.Tasks;

namespace RelatedPages.Controllers
{
    [Route("api/[controller]")]
    public class WikiWalksController : Controller
    {
        private readonly AllWorsGetter allWorsGetter;
        private readonly AllCategoriesGetter allCategoriesGetter;

        public WikiWalksController(AllWorsGetter allWorsGetter, AllCategoriesGetter allCategoriesGetter)
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
            var con = new DBCon();
            var pages = new List<Page>();

            string sql = "select wordId from Category where category like @category;";

            var result = con.ExecuteSelect(sql, new Dictionary<string, object[]> { { "@category", new object[2] { SqlDbType.NVarChar, category } } });

            result.ForEach((e) =>
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
        public IEnumerable<Page> getWordsForCategoryWithoutSnippet(string category)
        {
            var con = new DBCon();
            var pages = new List<Page>();

            string sql = "select wordId from Category where category like @category;";

            var result = con.ExecuteSelect(sql, new Dictionary<string, object[]> { { "@category", new object[2] { SqlDbType.NVarChar, category } } });

            result.ForEach((e) =>
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

        [HttpGet("[action]")]
        public async Task<object> getRelatedArticles(int wordId)
        {
            if (wordId <= 0) return new { };

            var con = new DBCon();
            var ps = new List<Page>();

            var result = con.ExecuteSelect(@"
select w.wordId, w.word, wr.snippet from Word as w
inner join
(select top(500) sourceWordId, snippet from WordReference where targetWordId = @wordId)
as wr
on w.wordId = wr.sourceWordId;
", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } });

            await Task.WhenAll(result.Select((e) => Task.Run(() =>
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
            })));

            var pages = ps.OrderByDescending(p => p.referenceCount).ToList();

            return new { pages };
        }

        [HttpGet("[action]")]
        public async Task<object> getRelatedCategories(int wordId)
        {
            if (wordId <= 0) return new { };

            var con = new DBCon();
            var categories = new List<Category>();

            var result = con.ExecuteSelect("select category from Category where wordId = @wordId;", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } });
            await Task.WhenAll(result.Select((f) => Task.Run(() =>
            {
                var c = allCategoriesGetter.getCategories().FirstOrDefault(ca => ca.category == (string)f["category"]);
                if (c != null)
                {
                    categories.Add(c);
                }
            })));

            return new { categories };
        }
    }
}

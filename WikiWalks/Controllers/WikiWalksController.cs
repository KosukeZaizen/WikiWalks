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
        public IEnumerable<Page> getAllWords()
        {
            return allWorsGetter.getPages();
        }

        [HttpGet("[action]")]
        public IEnumerable<object> getAllCategories()
        {
            return allCategoriesGetter.getCategories();
        }

        [HttpGet("[action]")]
        public IEnumerable<Page> getWordsForCategory(string category)
        {
            var con = new DBCon();
            var pages = new List<Page>();

            string sql = @"
select targetWordId as wordId,
LTRIM(max(case when targetWordId = sourceWordId then snippet else ' ' + snippet end)) as snippet
from WordReference r 
where exists (select * from category c where category like @category and r.targetWordId = c.wordId)
group by targetWordId;
";

            var result = con.ExecuteSelect(sql, new Dictionary<string, object[]> { { "@category", new object[2] { SqlDbType.NVarChar, category } } });

            result.ForEach((e) =>
            {
                var page = allWorsGetter.getPages().FirstOrDefault(w => w.wordId == (int)e["wordId"]);
                if (page != null)
                {
                    page.snippet = (string)e["snippet"];
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

            string sql = "select wordId from category where category like @category;";

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
        public async Task<object> getRelatedArticles(int wordId)
        {
            if (wordId <= 0) return new { };

            Task<List<Page>> pagesTask = Task.Run(() =>
            {
                var con = new DBCon();
                var ps = new List<Page>();

                var result = con.ExecuteSelect(@"
select firstRef.sourceWordId, firstRef.word, firstRef.snippet, count(wwrr.targetWordId) as cnt
from WordReference as wwrr
right outer join (
select wr.sourceWordId, w.word, wr.snippet 
from (
select sourceWordId, snippet from WordReference where targetWordId = @wordId
) as wr 
inner join Word as w 
on wr.sourceWordId = w.wordId 
) as firstRef
on firstRef.sourceWordId = wwrr.targetWordId
group by firstRef.sourceWordId, firstRef.word, firstRef.snippet
order by cnt desc;
", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } });

                result.ForEach((e) =>
                {
                    var page = new Page();
                    page.wordId = (int)e["sourceWordId"];

                    page.word = (string)e["word"];
                    page.snippet = (string)e["snippet"];
                    page.referenceCount = (int)e["cnt"];

                    ps.Add(page);
                });
                return ps;
            });

            Task<List<Category>> categoriesTask = Task.Run(() =>
            {
                var con = new DBCon();
                var cs = new List<Category>();

                var result = con.ExecuteSelect("select category from Category where wordId = @wordId", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } });
                result.ForEach((f) =>
                {
                    var c = allCategoriesGetter.getCategories().FirstOrDefault(ca => ca.category == (string)f["category"]);
                    if (c != null)
                    {
                        cs.Add(c);
                    }
                });
                return cs;
            });

            Task<string> wordTask = Task.Run(() =>
            {
                var con = new DBCon();
                var result = con.ExecuteSelect("select word from Word where wordId = @wordId;", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } });
                return (string)result.FirstOrDefault()["word"];
            });


            var word = await wordTask;
            var categories = await categoriesTask;
            var pages = await pagesTask;
            return new { wordId, word, pages, categories };
        }
    }
}

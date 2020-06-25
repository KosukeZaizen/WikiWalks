using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using RelatedPages.Models;
using System.Linq;

namespace RelatedPages.Controllers
{
    [Route("api/[controller]")]
    public class WikiWalksController : Controller
    {
        [HttpGet("[action]")]
        public IEnumerable<object> getAllCategories()
        {
            var con = new DBCon();
            var l = new List<object>();

            var result = con.ExecuteSelect(@"
select category, count(*) as cnt 
from (
	select c.category from Category as c 
	inner join WordReference as r 
	on c.wordId = r.targetWordId 
	group by c.wordId, c.category
	having count(*) > 4
) as rel
group by category 
order by cnt desc;
");

            result.ForEach((e) =>
            {
                var category = (string)e["category"];
                var cnt = (int)e["cnt"];

                l.Add(new { category, cnt });
            });

            return l;
        }

        [HttpGet("[action]")]
        public IEnumerable<Page> getWordsForCategory(string category)
        {
            var con = new DBCon();
            var pages = new List<Page>();

            string sql = @"
select wordsForCategory.wordId, wordsForCategory.word, 
max(case when wr.targetWordId = wr.sourceWordId then wr.snippet else ' ' + wr.snippet end) as snippet, 
count(wr.targetWordId) as cnt 
from
(
select c.wordId, w.word
from category as c 
inner join word as w 
on c.wordId = w.wordId 
and c.category like @category
) as wordsForCategory
left outer join WordReference as wr
on wordsForCategory.wordId = wr.targetWordId
group by wordsForCategory.wordId, wordsForCategory.word
order by cnt desc
;";

            var result = con.ExecuteSelect(sql, new Dictionary<string, object[]> { { "@category", new object[2] { SqlDbType.NVarChar, category } } });

            result.ForEach((e) =>
            {
                var page = new Page();
                page.wordId = (int)e["wordId"];
                page.word = (string)e["word"];
                page.snippet = (string)e["snippet"];
                page.referenceCount = (int)e["cnt"];

                pages.Add(page);
            });

            return pages;
        }

        [HttpGet("[action]")]
        public object getRelatedArticles(int wordId)
        {
            if (wordId <= 0) return new { };

            var con = new DBCon();
            var pages = new List<Page>();
            var categories = new List<object>();

            var result1 = con.ExecuteSelect($"select word from word where wordId = @wordId;", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } });
            string word = (string)result1.FirstOrDefault()["word"];

            var result2 = con.ExecuteSelect(@"
select firstRef.sourceWordId, firstRef.word, firstRef.snippet, count(wwrr.targetWordId) as cnt 
from WordReference as wwrr
right outer join (
select wr.sourceWordId, w.word, wr.snippet 
from WordReference as wr 
inner join word as w 
on wr.sourceWordId = w.wordId 
and targetWordId = @wordId
) as firstRef
on firstRef.sourceWordId = wwrr.targetWordId
group by firstRef.sourceWordId, firstRef.word, firstRef.snippet
order by cnt desc;
", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } });

            result2.ForEach((e) =>
            {
                var page = new Page();
                page.wordId = (int)e["sourceWordId"];

                page.word = (string)e["word"];
                page.snippet = (string)e["snippet"];
                page.referenceCount = (int)e["cnt"];

                pages.Add(page);
            });

            var result3 = con.ExecuteSelect(@"
select category, count(*) as cnt 
from (
	select wordId, category, count(*) as cnt1 from 
	(
	select wordId, category from Category where category in (select category from Category where wordId = @wordId)
	) as c 
	inner join WordReference as r 
	on c.wordId = r.targetWordId 
	group by wordId, category
	having count(*) > 4
) as rel
group by category
order by cnt desc;
", new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } });
            result3.ForEach((f) =>
            {
                categories.Add(new
                {
                    category = (string)f["category"],
                    cnt = (int)f["cnt"]
                });
            });

            return new { wordId, word, pages, categories };
        }

        [HttpGet("[action]")]
        public IEnumerable<Page> getAllWords()
        {
            var con = new DBCon();
            var pages = new List<Page>();

            string sql = @"
select w.wordId, w.word, count(*) as cnt from word as w
inner join WordReference as wr
on w.wordId = wr.targetWordId
group by w.wordId, w.word
having count(*) > 4
order by cnt desc;
";

            var result = con.ExecuteSelect(sql);

            result.ForEach((e) =>
            {
                var page = new Page();
                page.wordId = (int)e["wordId"];
                page.word = (string)e["word"];
                page.referenceCount = (int)e["cnt"];

                pages.Add(page);
            });

            return pages;
        }
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RelatedPages.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Web;
using System;
using System.Data;
using System.Net.Http;
using Newtonsoft.Json;
using Z_Apps.Models.SystemBase;
using System.Text.RegularExpressions;

namespace WikiWalks
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            var allWorsGetter = new AllWordsGetter();
            services.AddSingleton(allWorsGetter);

            var allCategoriesGetter = new AllCategoriesGetter(allWorsGetter);
            services.AddSingleton(allCategoriesGetter);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, AllWordsGetter allWorsGetter, AllCategoriesGetter allCategoriesGetter)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            var options = new RewriteOptions().AddRedirect("(.*)/$", "$1");
            app.UseRewriter(options);

            app.Use(async (context, next) =>
            {
                string url = context.Request.Path.Value;
                if (url.EndsWith("sitemap.xml"))
                {
                    var siteMapService = new SiteMapService(allWorsGetter, allCategoriesGetter);
                    string resultXML = siteMapService.GetSiteMapText(false, 0);
                    await context.Response.WriteAsync(resultXML);
                }
                else if (Regex.IsMatch(url, "sitemap[1-9][0-9]*.xml"))
                {
                    var siteMapService = new SiteMapService(allWorsGetter, allCategoriesGetter);
                    int number = Int32.Parse(Regex.Replace(url, @"[^0-9]", ""));
                    string resultXML = siteMapService.GetSiteMapText(false, number);
                    await context.Response.WriteAsync(resultXML);
                }
                else
                {
                    await next.Invoke();
                }
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }

    public class AllWordsGetter
    {
        private List<Page> pages = new List<Page>();
        private List<Page> newPages = new List<Page>();
        private int randomLimit = 5;

        public IEnumerable<Page> getPages()
        {
            return pages;
        }

        public void addNewPages()
        {
            Random random = new Random();
            for (int i = 0; i < random.Next(1, randomLimit); i++)
            {
                if (newPages.Count() > 0)
                {
                    pages.Add(newPages[0]);
                    newPages.RemoveAt(0);
                }
            }
            pages = pages.OrderByDescending(p => p.referenceCount).ToList();
        }

        public void hurryToSetAllPages()
        {
            try
            {
                DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enPage, true); //開始記録

                var con = new DBCon();
                var allPages = new List<Page>();

                string sql = @"
select
wr1.wordId,
wr1.word,
wr1.cnt,
isnull(
	(select top(1) snippet from WordReference wr3 where wr3.sourceWordId = wr3.targetWordId and wr3.sourceWordId = wr1.wordId),
	(select top(1) snippet from WordReference wr2 where wr2.sourceWordId = wr1.wordId)
) as snippet
from (
		select w.wordId, w.word, wr.cnt from Word as w
		inner join (
			select targetWordId, count(targetWordId) cnt
			from WordReference
			group by targetWordId having count(targetWordId) > 4
		) as wr
		on w.wordId = wr.targetWordId
	) as wr1
;";

                var result = con.ExecuteSelect(sql, null, 60 * 60 * 6); //タイムアウト６時間

                result.ForEach((e) =>
                {
                    var page = new Page();
                    page.wordId = (int)e["wordId"];
                    page.word = (string)e["word"];
                    page.referenceCount = (int)e["cnt"];
                    page.snippet = (string)e["snippet"];

                    allPages.Add(page);
                });

                pages = allPages.OrderByDescending(p => p.referenceCount).ToList();

                DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enPage, false); //終了記録
            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(1000 * 60);//DBへの負荷を考慮してSleep

                //DBにエラー内容書き出し
                var con = new DBCon();
                con.ExecuteUpdate($@"
INSERT INTO Log VALUES (
        DATEADD(HOUR, 9, GETDATE()), 
        N'WikiWalks-hurryToSetAllPages Message: {ex.Message.Replace("'", "")} StackTrace: {ex.StackTrace.Replace("'", "")}'
);
                ");

                hurryToSetAllPages();
            }
        }

        public void setPagesForDebug()
        {
            Task.Run(async () =>
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var res = await client.GetAsync(@"https://wiki.lingual-ninja.com/api/WikiWalks/getPartialWords?num=3000");
                        var result = await res.Content.ReadAsStringAsync();
                        pages = JsonConvert.DeserializeObject<List<Page>>(result);

                    }
                }
                catch (Exception ex)
                {
                    var e = ex;
                }
            });
        }


        public async Task setAllPagesAsync()
        {
            DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enPage, true); //開始記録

            var con = new DBCon();
            var allPages = new List<Page>();

            var min = 2680;// 2020-08-01確認
            var max = (int)con.ExecuteSelect("select max(wordId) as max from Word;").FirstOrDefault()["max"];

            string sqlForCnt = "select count(targetWordId) as cnt from WordReference where targetWordId = @wordId;";
            string sqlForEachWord = @"
select
wr1.word,
isnull(
	(select top(1) snippet from WordReference wr3 where wr3.sourceWordId = wr3.targetWordId and wr3.sourceWordId = wr1.wordId),
	(select top(1) snippet from WordReference wr2 where wr2.sourceWordId = wr1.wordId)
) as snippet
from (
	select wordId, word
	from Word
	where wordId = @wordId
) as wr1
;";

            await Task.Delay(1000 * 10);
            for (var wordId = min; wordId <= max; wordId++)
            {
                var d = wordId - min;
                if (d < 1000)
                {
                    //前半に大きな負荷がかかっているように見受けられるため、前半の待機を長めに
                    await Task.Delay(2003 - (d * 2));
                }
                else
                {
                    await Task.Delay(3);
                }

                int count = (int)con.ExecuteSelect(
                        sqlForCnt,
                        new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } }
                        ).FirstOrDefault()["cnt"];

                if (count > 4)
                {
                    await Task.Delay(50);
                    Page page = new Page
                    {
                        wordId = wordId,
                        referenceCount = count
                    };

                    var resultForEachWord = con.ExecuteSelect(
                            sqlForEachWord,
                            new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } }
                            );
                    var wordInfo = resultForEachWord.FirstOrDefault();
                    if (wordInfo != null)
                    {
                        page.word = (string)wordInfo["word"];
                        page.snippet = (string)wordInfo["snippet"];
                    }

                    allPages.Add(page);
                    await Task.Delay(50);
                }
            }

            int remainingNewPagesCount = newPages.Count();
            if (remainingNewPagesCount <= 0)
            {
                if (randomLimit > 1)
                {
                    randomLimit--;
                }
            }
            else
            {
                if (randomLimit < 5)
                {
                    randomLimit++;
                }
            }

            //前回と同じwordIdのページのみ更新（この時点では新規追加なし）
            pages = allPages
                .Where(p =>
                    pages.Any(oldPage => oldPage.wordId == p.wordId) ||
                    newPages.Any(oldPage => oldPage.wordId == p.wordId)
                )
                .OrderByDescending(p => p.referenceCount)
                .ToList();

            //新たに追加されているページを格納
            //（サイトマップへの問い合わせがある度に、上記のpagesに移していく）
            newPages = allPages
                .Where(p => !pages.Any(oldPage => oldPage.wordId == p.wordId))
                .ToList();

            DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enPage, false); //終了記録
        }
    }

    public class AllCategoriesGetter
    {
        private IEnumerable<Category> categories = new List<Category>();
        private AllWordsGetter allWordsGetter;

        public AllCategoriesGetter(AllWordsGetter allWordsGetter)
        {
            try
            {
                this.allWordsGetter = allWordsGetter;

#if DEBUG
                //デバッグ時
                allWordsGetter.setPagesForDebug();
                System.Threading.Thread.Sleep(1000 * 5);//5秒Sleep
                setCategoriesForDebug();
#else
                //本番時
                Task.Run(() => {
                    allWordsGetter.hurryToSetAllPages();
                    System.Threading.Thread.Sleep(1000 * 5);//DBへの負荷を考慮して5秒Sleep
                    hurryToSetAllCategories();
                });

                Task.Run(async () =>
                {
                    await Task.Delay(1000 * 60 * 30);

                    while (true)
                    {
                        await Task.Delay(1000 * 60);

                        if (DateTime.Now.Minute == 30)
                        {
                            try
                            {
                                await allWordsGetter.setAllPagesAsync();
                            }
                            catch (Exception ex)
                            {
                                //
                            }

                            await Task.Delay(1000 * 60 * 5);

                            try
                            {
                                await setAllCategoriesAsync();
                            }
                            catch (Exception ex)
                            {
                                //
                            }

                            try
                            {
                                //バッチが動いてなければ起動
                                StartBatch();
                            }
                            catch (Exception ex)
                            {
                                //
                            }
                        }
                    }
                });
#endif
            }
            catch (Exception ex) { }
        }

        private async void StartBatch()
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(@"https://wiki-bat.azurewebsites.net/");
                string msg = await response.Content.ReadAsStringAsync();
            }
        }

        public IEnumerable<Category> getCategories()
        {
            return categories;
        }

        private void hurryToSetAllCategories()
        {
            try
            {
                DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enCategory, true); //開始記録

                var con = new DBCon();
                var l = new List<Category>();

                var result = con.ExecuteSelect(@"
select category, count(*) as cnt 
from Category C
inner join (select targetWordId from WordReference group by targetWordId having count(targetWordId) > 4) as W
on W.targetWordId = C.wordId 
group by category
;", null, 60 * 60 * 6);// タイムアウト６時間

                result.ForEach((e) =>
                {
                    var c = new Category();
                    c.category = (string)e["category"];
                    c.cnt = (int)e["cnt"];

                    l.Add(c);
                });

                categories = l.OrderByDescending(c => c.cnt).ToList();

                DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enCategory, false); //終了記録
            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(1000 * 60);//DBへの負荷を考慮してSleep

                //DBにエラー内容書き出し
                var con = new DBCon();
                con.ExecuteUpdate($@"
INSERT INTO Log VALUES (
        DATEADD(HOUR, 9, GETDATE()), 
        N'WikiWalks-hurryToSetAllCategories Message: {ex.Message.Replace("'", "")} StackTrace: {ex.StackTrace.Replace("'", "")}'
);
                ");

                hurryToSetAllCategories();
            }
        }

        private void setCategoriesForDebug()
        {
            Task.Run(async () =>
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var res = await client.GetAsync(@"https://wiki.lingual-ninja.com/api/WikiWalks/getPartialCategories?num=3000");
                        var result = await res.Content.ReadAsStringAsync();
                        categories = JsonConvert.DeserializeObject<List<Category>>(result);
                    }
                }
                catch (Exception ex)
                {
                    var e = ex;
                }
            });

        }


        private async Task setAllCategoriesAsync()
        {
            DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enCategory, true); //開始記録

            var con = new DBCon();
            var l = new List<Category>();

            var pages = allWordsGetter.getPages().ToList();

            var hashCategories = new HashSet<string>();
            foreach (var page in pages)
            {
                await Task.Delay(10);
                con.ExecuteSelect(
                        "select category from Category where wordId = @wordId;",
                        new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, page.wordId } } }
                ).ForEach(cat =>
                {
                    hashCategories.Add((string)cat["category"]);
                });
            }

            await Task.Delay(1000 * 45);
            foreach (var cat in hashCategories)
            {
                await Task.Delay(10);

                var c = new Category();
                c.category = cat;

                c.cnt = con.ExecuteSelect(
                    "select wordId from Category where category like @category;",
                    new Dictionary<string, object[]> { { "@category", new object[2] { SqlDbType.NVarChar, c.category } } }
                    )
                .Count((a) => pages.Any(p => p.wordId == (int)a["wordId"]));

                if (c.cnt > 0)
                {
                    l.Add(c);
                }
            }

            categories = l.OrderByDescending(c => c.cnt).ToList();

            DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enCategory, false); //終了記録
        }
    }
}
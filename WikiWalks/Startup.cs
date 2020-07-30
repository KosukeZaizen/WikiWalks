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

            //一旦ロックを解除
            //DB_Util.releaseLock();

            var allWorsGetter = new AllWorsGetter();
            services.AddSingleton(allWorsGetter);

            var allCategoriesGetter = new AllCategoriesGetter(allWorsGetter);
            services.AddSingleton(allCategoriesGetter);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, AllWorsGetter allWorsGetter, AllCategoriesGetter allCategoriesGetter)
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
                    var domain = "https://wiki.lingual-ninja.com";
                    var lstSitemap = new List<Dictionary<string, string>>();

                    //top page
                    var dic1 = new Dictionary<string, string>();
                    dic1["loc"] = domain;
                    lstSitemap.Add(dic1);

                    //all keywords page
                    var dicAll = new Dictionary<string, string>();
                    dicAll["loc"] = domain + "/all";
                    lstSitemap.Add(dicAll);

                    //category page
                    IEnumerable<string> allCategories = allCategoriesGetter.getCategories().Select(c => c.category);
                    foreach (var category in allCategories)
                    {
                        var dic2 = new Dictionary<string, string>();
                        dic2["loc"] = domain + "/category/" + HttpUtility.UrlEncode(category.Replace(" ", "_")).Replace("%27", "'");
                        lstSitemap.Add(dic2);
                    }

                    //word page
                    IEnumerable<int> allWordId = allWorsGetter.getPages().Select(p => p.wordId);
                    foreach (var wordId in allWordId)
                    {
                        var dicWordId = new Dictionary<string, string>();
                        dicWordId["loc"] = domain + "/word/" + HttpUtility.UrlEncode(wordId.ToString());
                        lstSitemap.Add(dicWordId);
                    }

                    string resultXML = RegisterSitemap(lstSitemap);

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

        public string RegisterSitemap(IEnumerable<Dictionary<string, string>> sitemapItems)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("\n");
            sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
            sb.Append("\n");

            foreach (var item in sitemapItems)
            {
                sb.Append("  <url>");
                sb.Append("\n");

                sb.Append("    <loc>");
                sb.Append(item["loc"]);
                sb.Append("</loc>");
                sb.Append("\n");

                sb.Append("  </url>");
                sb.Append("\n");
            }
            sb.Append("</urlset>");

            return sb.ToString();
        }
    }

    public class AllWorsGetter
    {
        private IEnumerable<Page> pages = new List<Page>();
        public AllWorsGetter()
        {
            hurryToSetAllPages();

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(1000 * 60);

                        if (DateTime.Now.Minute == 30)
                        {
                            await setAllPagesAsync();
                        }
                    }
                    catch (Exception ex) { }
                }
            });
        }

        public IEnumerable<Page> getPages()
        {
            return pages;
        }

        private void hurryToSetAllPages()
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

            var result = con.ExecuteSelect(sql);

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


        private async Task setAllPagesAsync()
        {
            DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enPage, true); //開始記録

            var con = new DBCon();
            var allPages = new List<Page>();

            var min = (int)con.ExecuteSelect("select min(wordId) as min from Word;").FirstOrDefault()["min"];
            await Task.Delay(1000 * 10);
            var max = (int)con.ExecuteSelect("select max(wordId) as max from Word;").FirstOrDefault()["max"];

            string sqlForCnt = "select count(*) as cnt from WordReference where targetWordId = @wordId";
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
            for (var i = min; i <= max; i++)
            {
                await Task.Delay(1);
                var wordId = i;
                var count = (int)con.ExecuteSelect(
                        sqlForCnt,
                        new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } }
                        ).FirstOrDefault()["cnt"];

                if (count > 4)
                {
                    await Task.Delay(3);
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
                }
            }

            pages = allPages.OrderByDescending(p => p.referenceCount).ToList();

            DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enPage, false); //終了記録
        }
    }

    public class AllCategoriesGetter
    {
        private IEnumerable<Category> categories = new List<Category>();
        private AllWorsGetter allWorsGetter;

        public AllCategoriesGetter(AllWorsGetter allWorsGetter)
        {
            this.allWorsGetter = allWorsGetter;

            hurryToSetAllCategories();

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(1000 * 60);

                        if (DateTime.Now.Minute == 40)
                        {
                            await setAllCategoriesAsync();
                        }
                    }
                    catch (Exception ex) { }
                }
            });
        }

        public IEnumerable<Category> getCategories()
        {
            return categories;
        }

        private void hurryToSetAllCategories()
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
;");

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


        private async Task setAllCategoriesAsync()
        {
            DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enCategory, true); //開始記録

            var con = new DBCon();
            var l = new List<Category>();

            var pages = allWorsGetter.getPages().ToList();

            var hashCategories = new HashSet<string>();
            foreach (var page in pages)
            {
                await Task.Delay(4);
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
                await Task.Delay(5);

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
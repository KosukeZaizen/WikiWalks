using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
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

            var allWorsGetter = new AllWorsGetter();
            services.AddSingleton(allWorsGetter);

            var allCategoriesGetter = new AllCategoriesGetter();
            services.AddSingleton(allCategoriesGetter);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
                    IEnumerable<string> allCategories = getAllCategories();
                    foreach (var category in allCategories)
                    {
                        var dic2 = new Dictionary<string, string>();
                        dic2["loc"] = domain + "/category/" + HttpUtility.UrlEncode(category.Replace(" ", "_"));
                        lstSitemap.Add(dic2);
                    }

                    //word page
                    IEnumerable<int> allWordId = getAllWordId();
                    foreach (var wordId in allWordId)
                    {
                        var dicWordId = new Dictionary<string, string>();
                        dicWordId["loc"] = domain + "/word/" + HttpUtility.UrlEncode(wordId.ToString());
                        lstSitemap.Add(dicWordId);
                    }

                    string resultXML = await RegisterSitemap(lstSitemap);

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

        public async Task<string> RegisterSitemap(IEnumerable<Dictionary<string, string>> sitemapItems)
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

        public IEnumerable<string> getAllCategories()
        {
            var con = new DBCon();
            var l = new List<string>();

            var result = con.ExecuteSelect(@"
select category, count(*) as cnt 
from (
	select wordId, category, count(*) as cnt1 from Category as c 
	inner join WordReference as r 
	on c.wordId = r.targetWordId 
	group by wordId, category
	having count(*) > 4
) as rel
group by category 
order by cnt desc;
");

            result.ForEach((e) =>
            {
                var category = (string)e["category"];

                l.Add(category);
            });

            return l;
        }

        public IEnumerable<int> getAllWordId()
        {
            var con = new DBCon();
            var pages = new List<int>();

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
                var word = (int)e["wordId"];
                pages.Add(word);
            });

            return pages;
        }

    }

    public class AllWorsGetter
    {
        private IEnumerable<Page> pages;
        public AllWorsGetter()
        {
            pages = new List<Page>();

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(1000 * 60 * 10);
                        setAllPages();
                    }
                    catch (Exception ex) { }
                }
            });

            setAllPages();
        }

        public IEnumerable<Page> getPages()
        {
            return pages;
        }

        private void setAllPages()
        {
            var con = new DBCon();
            var allPages = new List<Page>();

            string sql = @"
select w.wordId, w.word, wr.cnt from Word as w
inner join (
select targetWordId, count(targetWordId) cnt from WordReference group by targetWordId having count(targetWordId) > 4
) as wr
on w.wordId = wr.targetWordId
order by cnt desc;
";

            var result = con.ExecuteSelect(sql);

            result.ForEach((e) =>
            {
                var page = new Page();
                page.wordId = (int)e["wordId"];
                page.word = (string)e["word"];
                page.referenceCount = (int)e["cnt"];

                allPages.Add(page);
            });

            pages = allPages;
        }
    }

    public class AllCategoriesGetter
    {
        private IEnumerable<object> categories;
        public AllCategoriesGetter()
        {
            categories = new List<object>();

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(1000 * 60 * 10);
                        setAllCategories();
                    }
                    catch (Exception ex) { }
                }
            });

            setAllCategories();
        }

        public IEnumerable<object> getCategories()
        {
            return categories;
        }

        private void setAllCategories()
        {
            var con = new DBCon();
            var l = new List<object>();

            var result = con.ExecuteSelect(@"
select category, count(*) as cnt from Category C
where exists (select targetWordId from WordReference W where W.targetWordId = C.wordId group by targetWordId having count(targetWordId) > 4)
group by category
order by cnt desc;
");

            result.ForEach((e) =>
            {
                var category = (string)e["category"];
                var cnt = (int)e["cnt"];

                l.Add(new { category, cnt });
            });

            categories = l;
        }
    }
}

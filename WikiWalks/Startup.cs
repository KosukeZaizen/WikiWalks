using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RelatedPages.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Data;
using System.Net.Http;
using Z_Apps.Models.SystemBase;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using RelatedPages.Controllers;

namespace WikiWalks
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration
        {
            get;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
            });

            var allWorsGetter = new AllWordsGetter();
            services.AddSingleton(allWorsGetter);

            var allCategoriesGetter = new AllCategoriesGetter(allWorsGetter);
            services.AddSingleton(allCategoriesGetter);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AllWordsGetter allWorsGetter, AllCategoriesGetter allCategoriesGetter)
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

            app.UseResponseCompression();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller}/{action=Index}/{id?}");
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
                var cachedPage = AllDataCache.GetCachePage();
                if (cachedPage != null)
                {
                    pages = cachedPage;
                }
            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(1000 * 60);//DBへの負荷を考慮してSleep

                //DBにエラー内容書き出し
                ErrorLog.InsertErrorLog(ex.Message);

                hurryToSetAllPages();
            }
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

            await Task.Delay(1000 * 30);
            for (var wordId = min; wordId <= max; wordId++)
            {
                var d = wordId - min;
                if (d < 1000)
                {
                    //前半に大きな負荷がかかっているように見受けられるため、前半の待機を長めに
                    await Task.Delay(2500 - (d * 2));
                }
                else
                {
                    await Task.Delay(500);
                }

                int count = (int)con.ExecuteSelect(
                        sqlForCnt,
                        new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, wordId } } }
                        ).FirstOrDefault()["cnt"];

                if (count > 4)
                {
                    await Task.Delay(500);
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
                    await Task.Delay(500);
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

            var pagesWithJapan = new List<Page>();
            var pagesWithoutJapan = new List<Page>();
            foreach (var newPage in allPages)
            {
                // 前のpagesに既に同じものがあるか
                var oldSamePage = pages
                                    .FirstOrDefault(pa => pa.wordId == newPage.wordId);
                if (oldSamePage != null)
                {
                    newPage.isAboutJapan = oldSamePage.isAboutJapan;
                    pagesWithJapan.Add(newPage);
                }
                else
                {
                    // １つ前のnewPagesに既に同じものがあるか
                    var previousSamePage = newPages
                                    .FirstOrDefault(pa => pa.wordId == newPage.wordId);
                    if (previousSamePage != null)
                    {
                        newPage.isAboutJapan = previousSamePage.isAboutJapan;
                        pagesWithJapan.Add(newPage);
                    }
                    else
                    {
                        // 今回初めて取得したページ
                        pagesWithoutJapan.Add(newPage);
                    }
                }
            }

            pages = pagesWithJapan
                        .OrderByDescending(p => p.referenceCount)
                        .ToList();

            //新たに追加されているページを格納
            //（サイトマップへの問い合わせがある度に、上記のpagesに移していく）
            newPages = pagesWithoutJapan;

            DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enPage, false); //終了記録
        }
    }

    public class AllCategoriesGetter
    {
        private IEnumerable<Category> categories = new List<Category>();
        private IEnumerable<Category> japaneseCategories = new List<Category>();
        private AllWordsGetter allWordsGetter;

        public AllCategoriesGetter(AllWordsGetter allWordsGetter)
        {
            try
            {
                this.allWordsGetter = allWordsGetter;

                Task.Run(() =>
                {
                    allWordsGetter.hurryToSetAllPages();
                    System.Threading.Thread.Sleep(1000 * 5);//DBへの負荷を考慮して5秒Sleep
                    hurryToSetAllCategories();
                });

#if DEBUG
                //デバッグ時は以下の処理を起動しない
                return;
#endif

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
                                ErrorLog.InsertErrorLog("allWordsGetter.setAllPagesAsync(); " + ex.Message);
                            }

                            await Task.Delay(1000 * 60 * 5);

                            try
                            {
                                await setAllCategoriesAsync();
                            }
                            catch (Exception ex)
                            {
                                ErrorLog.InsertErrorLog("setAllCategoriesAsync(); " + ex.Message);
                            }

                            try
                            {
                                //バッチが動いてなければ起動
                                StartBatch();
                            }
                            catch (Exception ex)
                            {
                                ErrorLog.InsertErrorLog("バッチが動いてなければ起動 " + ex.Message);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                ErrorLog.InsertErrorLog(ex.Message);
            }
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

        public IEnumerable<Category> getJapaneseCategories()
        {
            return japaneseCategories;
        }
        private void hurryToSetAllCategories()
        {
            try
            {
                var cachedCategory = AllDataCache.GetCacheCategory();
                if (cachedCategory != null)
                {
                    categories = cachedCategory;
                    japaneseCategories = cachedCategory
                        .Where(c => c.category.ToLower().Contains("japan"));
                }
            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(1000 * 60);//DBへの負荷を考慮してSleep

                //DBにエラー内容書き出し
                var con = new DBCon();

                ErrorLog.InsertErrorLog(ex.Message);

                hurryToSetAllCategories();
            }
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
                await Task.Delay(500);
                var isAboutJapan = false;
                con.ExecuteSelect(
                        "select category from Category where wordId = @wordId;",
                        new Dictionary<string, object[]> { { "@wordId", new object[2] { SqlDbType.Int, page.wordId } } }
                ).ForEach(cat =>
                {
                    var categ = (string)cat["category"];
                    if (categ.ToLower().Contains("japan"))
                    {
                        isAboutJapan = true;
                    }
                    hashCategories.Add(categ);
                });
                page.isAboutJapan = isAboutJapan;
            }

            await Task.Delay(1000 * 45);
            foreach (var cat in hashCategories)
            {
                await Task.Delay(500);

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
            japaneseCategories = categories
                .Where(c => c.category.ToLower().Contains("japan"));

            AllDataCache.SaveCache(AllDataCache.Keys.WikiCategory, categories);
            AllDataCache.SaveCache(AllDataCache.Keys.WikiPages, pages);

            DB_Util.RegisterLastTopUpdate(DB_Util.procTypes.enCategory, false); //終了記録
        }
    }
}
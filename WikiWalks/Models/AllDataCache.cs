using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace RelatedPages.Models {

    public class AllDataCache {

        public enum Keys {
            WikiCategory,
            WikiPages
        }

        private static string GetKeyString(Keys key) {

            switch (key) {
                case Keys.WikiCategory:
                    return "WikiCategory";
                case Keys.WikiPages:
                    return "WikiPages";
            }
            return "";
        }

        public static void SaveCache(Keys key, object data) {

            try {
                var con = new DBCon();

                var json = System.Text.Json.JsonSerializer.Serialize(data);

                con.ExecuteUpdate(@"
update AllDataCache
set cacheData = @json
where cacheKey = @key
;",
                    new Dictionary<string, object[]> {
                            { "@json", new object[2] { SqlDbType.NVarChar, json } },
                            { "@key", new object[2] { SqlDbType.NVarChar, GetKeyString(key) } }
                    },
                    60 * 60 * 6);// タイムアウト６時間
            } catch (Exception ex) {
                ErrorLog.InsertErrorLog(ex.Message);
            }
        }


        public static List<Category> GetCacheCategory() {
            try {
                var con = new DBCon();

                var result = con.ExecuteSelect(@"
select cacheData 
from AllDataCache 
where cacheKey = N'WikiCategory'
;"
                , null,
                60 * 60 * 6)// タイムアウト６時間
                .FirstOrDefault();

                if (result != null) {
                    return JsonConvert
                        .DeserializeObject<List<Category>>(
                            (string)result["cacheData"]
                        );
                }
            } catch (Exception ex) {
                ErrorLog.InsertErrorLog(ex.Message);
            }
            return null;
        }


        public static List<Page> GetCachePage() {
            try {
                var con = new DBCon();

                var result = con.ExecuteSelect(@"
select cacheData 
from AllDataCache 
where cacheKey = N'WikiPages'
;"
                , null,
                60 * 60 * 6)// タイムアウト６時間
                .FirstOrDefault();

                if (result != null) {
                    return JsonConvert
                                .DeserializeObject<List<Page>>(
                                    (string)result["cacheData"]
                                );
                }
            } catch (Exception ex) {
                ErrorLog.InsertErrorLog(ex.Message);
            }
            return null;
        }
    }
}
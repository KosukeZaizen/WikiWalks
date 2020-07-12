using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using RelatedPages.Models;

public class DB_Util
{
    private readonly DBCon con;
    public DB_Util(DBCon con)
    {
        this.con = con;
    }

    public List<string> GetAllTableNames()
    {
        //SQL文作成
        string sql = "SELECT *";
        sql += " FROM   sysobjects";
        sql += " WHERE  xtype = 'U'";
        sql += " ORDER BY name;";

        //List<Dictionary<string, Object>>型で取得
        var tables = con.ExecuteSelect(sql, null);

        var tableNames = new List<string>();
        foreach (var table in tables)
        {
            tableNames.Add((string)table["name"]);
        }
        return tableNames;
    }

    public List<Dictionary<string, object>> GetAllDataFromOneTable(string tableName)
    {
        //SQL文作成
        string sql = "SELECT *";
        sql += " FROM "+ tableName;

        //List<Dictionary<string, Object>>型で取得
        var records = con.ExecuteSelect(sql, null);

        return records;
    }

    public static async Task runHeavySqlAsync(Action proc)
    {
        var con = new DBCon();

        try
        {
            var isLocked = true;
            int count = 0;
            while (isLocked)
            {
                count++;

                string sqlToChek = "select isLocked from LockHeavySql";

                var checkResult = con.ExecuteSelect(sqlToChek).FirstOrDefault();

                isLocked = (bool)checkResult["isLocked"];

                if (isLocked)
                {
                    await Task.Delay(1000 * 30);
                }
            }

            //Lockをかける
            string sql = "update LockHeavySql set isLocked = 1;";
            con.ExecuteUpdate(sql);

            if (count > 1)
            {
                //待った直後はDBの負荷が下がっていない可能性があるので、処理実行前にさらに30秒待つ
                await Task.Delay(1000 * 30);
            }

            //処理実行
            proc();
        }
        finally
        {
            //Lock解除
            string sql = "update LockHeavySql set isLocked = 0;";
            con.ExecuteUpdate(sql);
        }
    }
}
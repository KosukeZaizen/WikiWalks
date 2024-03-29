using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace RelatedPages.Models {
    public class ErrorLog {
        public static void InsertErrorLog(string error) {
            try {
                // StackFrameクラスをインスタンス化する
                StackFrame objStackFrame = new StackFrame(1);// フレーム数1なら直接呼び出したメソッド

                string strClassName = objStackFrame.GetMethod().ReflectedType.FullName;// 呼び出し元のクラス名を取得する
                string strMethodName = objStackFrame.GetMethod().Name;// 呼び出し元のメソッド名を取得する



                var con = new DBCon();

                con.ExecuteUpdate(@"
insert into WikiEnErrorLog 
values (DATEADD(HOUR, 9, GETDATE()), @error);
;",
                    new Dictionary<string, object[]> {
                            { "@error", new object[2] { SqlDbType.NVarChar,
                                $"{strClassName}.{strMethodName}(): {error}"
                            } }
                    });
            } catch (Exception ex) { }
        }
    }
}
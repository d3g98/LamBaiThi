using FirebirdSql.Data.FirebirdClient;
using No1Lib.Db;
using No1Lib.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TVS_DT_TD.Controllers.Common;
using TVS_DT_TD.Controllers.Filter;

namespace TVS_DT_TD.Controllers
{
    [AuthFilter]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string id = DuLieuDangLam();
            if (id.Length > 0)
            {
                return RedirectToAction("LamBaiThi", "KiemTra", new { id = id });
            }
            return View();
        }

        public static string DuLieuDangLam()
        {
            string error = "", kq = "";
            Database db = DatabaseUtils.GetDatabase();
            db.TryConnect(out error);

            if (error.Length == 0)
            {
                try
                {
                    DataRow rUser = (DataRow)SessionManager.GetObject(Contants.USER_SESSION);
                    string DUNGVIENID = ConvertTo.String(rUser["ID"]);

                    FbCommand cmd = db.GetCommand("");
                    cmd.Parameters.Add("@DUNGVIENID", FbDbType.VarChar).Value = DUNGVIENID;
                    //Kiểm tra nếu có bài đang làm thì tuyển tới luôn
                    cmd.CommandText = @"SELECT TDOTTUYENDUNGTHIMAYCT.ID
FROM TDOTTUYENDUNGTHIMAYCT INNER JOIN TDOTTUYENDUNGCHITIET ON TDOTTUYENDUNGTHIMAYCT.TDOTTUYENDUNGCHITIETID = TDOTTUYENDUNGCHITIET.ID
AND COALESCE(GIOBATDAU, '') <> '' AND COALESCE(KETTHUC, 0) = 0 AND TDOTTUYENDUNGTHIMAYCT.DUNGVIENID = @DUNGVIENID";
                    kq = db.GetFirstFieldString(cmd);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                finally
                {
                    if (db != null && db.State == System.Data.ConnectionState.Open)
                    {
                        db.Disconnect();
                        db.Dispose();
                    }
                }
            }

            return kq;
        }
    }
}
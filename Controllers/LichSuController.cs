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
    public class LichSuController : Controller
    {
        public ActionResult Index()
        {
            string error = "";
            Database db = DatabaseUtils.GetDatabase();
            db.TryConnect(out error);

            DataTable dt = null;
            if (error.Length == 0)
            {
                try
                {
                    DataRow rUser = (DataRow)SessionManager.GetObject(Contants.USER_SESSION);
                    string DUNGVIENID = ConvertTo.String(rUser["ID"]);
                    FbCommand cmd = db.GetCommand("");
                    cmd.Parameters.Add("@DUNGVIENID", FbDbType.VarChar).Value = DUNGVIENID;
                    cmd.CommandText = @"SELECT TDOTTUYENDUNGTHIMAYCT.ID, FORMAT(GIOBATDAU, 'dd/MM/yyyy HH:mm') AS NGAY, DDETHI.NAME, TDOTTUYENDUNGTHIMAYCT.SOCAUTRALOIDUNG, TDOTTUYENDUNGTHIMAYCT.DIEM, SOCAU,
                    TDOTTUYENDUNGTHIMAYCT.DVONGTUYENDUNGID, TDOTTUYENDUNGTHIMAYCT.LOAIDIEM
                    FROM TDOTTUYENDUNGTHIMAYCT INNER JOIN DDETHI ON TDOTTUYENDUNGTHIMAYCT.DDETHIID = DDETHI.ID
                    WHERE KETTHUC = 30 AND EXISTS (SELECT * FROM TDOTTUYENDUNGCHITIET INNER JOIN TDOTTUYENDUNGTHIMAYCT ON TDOTTUYENDUNGCHITIETID = TDOTTUYENDUNGCHITIET.ID
                    AND TDOTTUYENDUNGTHIMAYCT.DUNGVIENID = @DUNGVIENID)";
                    dt = db.GetTable(cmd);

                    //Lấy tiêu đề vòng thi
                    DataTable dtVongTuyenDung = db.GetTable("SELECT ID, PARENTID, COTHIMON1, TENMONTHI1, COTHIMON2, TENMONTHI2, COTHIMON3, TENMONTHI3 FROM DVONGTUYENDUNG");
                    foreach (DataRow row in dt.Rows)
                    {
                        row["NAME"] = KiemTraController.LayTenMonThi(dtVongTuyenDung, ConvertTo.String(row["DVONGTUYENDUNGID"]), ConvertTo.Int(row["LOAIDIEM"]));
                    }
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
            if (error.Length > 0) ViewBag.error = error;
            if ((dt == null || dt.Rows.Count == 0) && error.Length == 0) error = "Lịch sử kiểm tra trống!";
            return View(dt);
        }
    }
}
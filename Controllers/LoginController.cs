using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FirebirdSql.Data.FirebirdClient;
using No1Lib.Db;
using No1Lib.Utils;
using TVS_DT_TD.Controllers.Common;

namespace TVS_DT_TD.Controllers
{
    public class LoginController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.username = "UV0077";
            ViewBag.password = "";
            return View();
        }

        [HttpPost]
        public ActionResult KiemTraDangNhap(string username, string password)
        {
            string error = "";
            base.Session["Temp"] = 1;

            if (ModelState.IsValid)
            {
                Database db = DatabaseUtils.GetDatabase();
                db.TryConnect(out error);

                if (error.Length == 0)
                {
                    try
                    {
                        FbCommand cmd = db.GetCommand("");
                        cmd.CommandText = "SELECT ID, NAME FROM DUNGVIEN WHERE COALESCE(CODE, '') = @CODE AND COALESCE(MATKHAU, '') = @MATKHAU";
                        cmd.Parameters.Add("@CODE", FbDbType.VarChar).Value = username.Trim();
                        cmd.Parameters.Add("@MATKHAU", FbDbType.VarChar).Value = password.Trim();
                        DataRow rUser = db.GetFirstRow(cmd);
                        if (rUser == null)
                        {
                            error = "Tài khoản hoặc mật khẩu không đúng";
                        }
                        else
                        {
                            SessionManager.SetObject(Contants.USER_SESSION, rUser);
                            return RedirectToAction("Index", "Home");
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
            }

            ViewBag.username = username;
            ViewBag.password = password;
            ViewBag.error = error;

            return View("Index");
        }

        public ActionResult Logout()
        {
            SessionManager.SetObject(Contants.USER_SESSION, null);
            return RedirectToAction("Index", "Home");
        }
    }
}
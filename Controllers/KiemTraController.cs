using FirebirdSql.Data.FirebirdClient;
using No1Lib.Db;
using No1Lib.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TVS_DT_TD.Controllers.Common;
using TVS_DT_TD.Controllers.Filter;

namespace TVS_DT_TD.Controllers
{
    [AuthFilter]
    public class KiemTraController : Controller
    {
        public ActionResult Index()
        {
            string id = HomeController.DuLieuDangLam();
            if (id.Length > 0)
            {
                return RedirectToAction("LamBaiThi", new { id = id });
            }
            return View();
        }

        //Danh sách bài thi chưa thi
        public ActionResult DanhSachBaiThi()
        {
            string error = "";
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
                    cmd.CommandText = @"SELECT TDOTTUYENDUNGTHIMAY.ID AS TDOTTUYENDUNGTHIMAYID, TDOTTUYENDUNGCHITIET.ID AS TDOTTUYENDUNGCHITIETID, TDOTTUYENDUNGTHIMAY.DDETHIID, DDETHI.NAME, DDETHI.SOCAU, DDETHI.THOIGIAN,
                    TDOTTUYENDUNGCHITIET.DVONGTUYENDUNGID, TDOTTUYENDUNGTHIMAY.LOAIDIEM, TDOTTUYENDUNGCHITIET.DUNGVIENID
                    FROM TDOTTUYENDUNGCHITIET INNER JOIN TDOTTUYENDUNGTHIMAY ON TDOTTUYENDUNGCHITIET.TDOTTUYENDUNGID = TDOTTUYENDUNGTHIMAY.TDOTTUYENDUNGID AND LANCUOI = 30
                    INNER JOIN DDETHI ON TDOTTUYENDUNGTHIMAY.DDETHIID = DDETHI.ID
                    AND((TDOTTUYENDUNGCHITIET.DVONGTUYENDUNGID = TDOTTUYENDUNGTHIMAY.DVONGTUYENDUNGID) OR(TDOTTUYENDUNGTHIMAY.DVONGTUYENDUNGID =
                    (SELECT PARENTID FROM DVONGTUYENDUNG A WHERE A.ID = TDOTTUYENDUNGCHITIET.DVONGTUYENDUNGID)))
                    WHERE NOT EXISTS
                    (SELECT * FROM TDOTTUYENDUNGTHIMAYCT WHERE TDOTTUYENDUNGTHIMAYCT.DUNGVIENID = TDOTTUYENDUNGCHITIET.DUNGVIENID
                    AND TDOTTUYENDUNGTHIMAYCT.LOAIDIEM = TDOTTUYENDUNGTHIMAY.LOAIDIEM
                    AND TDOTTUYENDUNGTHIMAYCT.TDOTTUYENDUNGID = TDOTTUYENDUNGTHIMAY.TDOTTUYENDUNGID
                    AND TDOTTUYENDUNGTHIMAYCT.DVONGTUYENDUNGID = TDOTTUYENDUNGTHIMAY.DVONGTUYENDUNGID)
                    AND DUNGVIENID = @DUNGVIENID ORDER BY DDETHI.NAME";
                    DataTable dt = db.GetTable(cmd);
                    //Lấy tiêu đề vòng thi
                    DataTable dtVongTuyenDung = db.GetTable("SELECT ID, PARENTID, COTHIMON1, TENMONTHI1, COTHIMON2, TENMONTHI2, COTHIMON3, TENMONTHI3 FROM DVONGTUYENDUNG");
                    foreach (DataRow row in dt.Rows)
                    {
                        row["NAME"] = LayTenMonThi(dtVongTuyenDung, ConvertTo.String(row["DVONGTUYENDUNGID"]), ConvertTo.Int(row["LOAIDIEM"]));
                    }
                    return PartialView(dt);
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
            return PartialView();
        }

        public static string LayTenMonThi(DataTable dtVongTuyenDung, string DVONGTUYENDUNGID, int LOAIDIEM)
        {
            string kq = "";
            //đệ quy lấy thằng cha
            DataRow row = dtVongTuyenDung.Select("ID='"+DVONGTUYENDUNGID+"'")[0];
            while (ConvertTo.String(row["PARENTID"]).Length  > 0)
            {
                row = dtVongTuyenDung.Select("ID='" + ConvertTo.String(row["PARENTID"]) + "'")[0];
            }

            string field = "TENMONTHI" + (LOAIDIEM + 1);
            kq = row[field].ToString();

            return kq;
        }

        [HttpPost]
        public ActionResult NoiQuy(string chitietid, string dethiid, string vongid, string ungvienid, int loaidiem)
        {
            string error = "";
            Database db = DatabaseUtils.GetDatabase();
            db.TryConnect(out error);

            DataRow row = null;
            if (error.Length == 0)
            {
                try
                {
                    FbCommand cmd = db.GetCommand("");
                    cmd.Parameters.Add("@TDOTTUYENDUNGCHITIETID", FbDbType.VarChar).Value = ConvertTo.String(chitietid);
                    cmd.Parameters.Add("@DDETHIID", FbDbType.VarChar).Value = ConvertTo.String(dethiid);
                    cmd.CommandText = "SELECT DDETHI.*, "+ ConvertTo.Int(loaidiem) +" AS LOAIDIEM, '"+ chitietid + "' AS CHITIETID, '"+ vongid + "' AS VONGID, '"+ ungvienid + "' AS UNGVIENID FROM DDETHI WHERE ID = @DDETHIID";
                    row = db.GetFirstRow(cmd);
                    if (row != null)
                    {
                        row["NOIQUYTHI"] = HttpUtility.HtmlDecode(ConvertTo.String(row["NOIQUYTHI"]));
                    }
                    else
                    {
                        error = "Mã bài thi không hợp lệ vui lòng thử lại";
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
            return View(row);
        }

        [HttpPost]
        public ActionResult LuuLuotThi(string chiTietId, string deThiId, string vongId, string ungVienId, int loaiDiem)
        {
            string error = "";
            Database db = DatabaseUtils.GetDatabase();
            db.TryConnect(out error);

            string kq = "";
            if (error.Length == 0)
            {
                try
                {
                    FbCommand cmd = db.GetCommand("SELECT * FROM TDOTTUYENDUNGCHITIET WHERE ID = @TDOTTUYENDUNGCHITIETID");
                    cmd.Parameters.Add("@TDOTTUYENDUNGCHITIETID", FbDbType.VarChar).Value = chiTietId;
                    DataRow rChiTiet = db.GetFirstRow(cmd);
                    if (rChiTiet == null)
                    {
                        kq = "ERR:Dữ liệu chi tiết không hợp lệ";
                    }
                    else
                    {
                        string id = Guid.NewGuid().ToString();
                        string parentId = GetParentId(db, vongId);
                        cmd.Parameters.Add("@ID", FbDbType.VarChar).Value = id;
                        cmd.Parameters.Add("@DDETHIID", FbDbType.VarChar).Value = deThiId;
                        cmd.Parameters.Add("@LOAIDIEM", FbDbType.VarChar).Value = loaiDiem;
                        cmd.Parameters.Add("@TDOTTUYENDUNGID", FbDbType.VarChar).Value = rChiTiet["TDOTTUYENDUNGID"];
                        cmd.Parameters.Add("@DVONGTUYENDUNGID", FbDbType.VarChar).Value = parentId;
                        cmd.Parameters.Add("@DUNGVIENID", FbDbType.VarChar).Value = ungVienId;

                        //Kiểm tra xem có dữ liệu chưa
                        cmd.CommandText = @"SELECT ID FROM TDOTTUYENDUNGTHIMAYCT WHERE TDOTTUYENDUNGID=@TDOTTUYENDUNGID AND DDETHIID=@DDETHIID AND DVONGTUYENDUNGID=@DVONGTUYENDUNGID AND DUNGVIENID=@DUNGVIENID AND LOAIDIEM=@LOAIDIEM AND COALESCE(KETTHUC, 0)=0";
                        string temp = db.GetFirstFieldString(cmd);
                        if (temp.Length > 0)
                        {
                            kq = temp;
                        }
                        else
                        {
                            cmd.CommandText = @"INSERT INTO TDOTTUYENDUNGTHIMAYCT(ID, STATUS, TIMECREATED, USERCREATEDID, TDOTTUYENDUNGID, DDETHIID, TDOTTUYENDUNGCHITIETID, LOAIDIEM, DVONGTUYENDUNGID, DUNGVIENID)
                            VALUES(@ID, 30, GETDATE(), 'PM_TuyenDung_DaoTao', @TDOTTUYENDUNGID, @DDETHIID, @TDOTTUYENDUNGCHITIETID, @LOAIDIEM, @DVONGTUYENDUNGID, @DUNGVIENID)";
                            db.ExecSql(cmd);
                            kq = id;
                        }
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
            if (!kq.StartsWith("ERR:") && error.Length > 0)
            {
                kq = "ERR:" + error;
            }
            return Content(kq);
        }

        public static string GetParentId(Database db, string vongId)
        {
            string kq = "";
            int count = 0;
            DataTable dtVongTuyenDung = db.GetTable("SELECT ID, PARENTID FROM DVONGTUYENDUNG");
            while (kq.Length == 0 && count < 100)
            {
                DataRow[] rows = dtVongTuyenDung.Select("ID='"+ vongId +"'");
                if (rows.Length == 0)
                {
                    kq = vongId;
                }
                else
                {
                    string parentid = ConvertTo.String(rows[0]["PARENTID"]);
                    if (parentid.Length == 0) kq = vongId;
                    else vongId = parentid;
                }
                count++;
            }
            return kq;
        }

        public ActionResult LamBaiThi(string id)
        {
            string error = "";
            Database db = DatabaseUtils.GetDatabase();
            db.TryConnect(out error);

            if (error.Length == 0)
            {
                try
                {
                    FbCommand cmd = db.GetCommand("");
                    //Lấy dữ liệu đề thi
                    cmd.Parameters.Add("@ID", FbDbType.VarChar).Value = id;
                    cmd.CommandText = "SELECT * FROM TDOTTUYENDUNGTHIMAYCT WHERE ID = @ID";
                    DataRow rowDotThi = db.GetFirstRow(cmd);

                    if (rowDotThi != null)
                    {
                        if (ConvertTo.Int(rowDotThi["KETTHUC"]) > 0)
                        {
                            return RedirectToAction("KetQua", new { id = id });
                        }

                        //tham so
                        bool dangLam = false;
                        int thoiGian = 0, soCau = 0;
                        DateTime fromDate = db.DbDateTime;
                        DataTable dtCauHoi = new DataTable();
                        DataTable dtDapAn = null;

                        string DDETHIID = ConvertTo.String(rowDotThi["DDETHIID"]);
                        string DVONGTUYENDUNGID = ConvertTo.String(rowDotThi["DVONGTUYENDUNGID"]);
                        cmd.Parameters.Add("@DDETHIID", FbDbType.VarChar).Value = DDETHIID;
                        cmd.Parameters.Add("@TDOTTUYENDUNGTHIMAYCTID", FbDbType.VarChar).Value = id;
                        cmd.Parameters.Add("@DVONGTUYENDUNGID", FbDbType.VarChar).Value = DVONGTUYENDUNGID;
                        cmd.CommandText = "SELECT * FROM DDETHI WHERE ID = @DDETHIID";
                        DataRow rowDeThi = db.GetFirstRow(cmd);
                        if (rowDeThi == null)
                        {
                            error = "Có lỗi trong quá trình tải đề thi, vui lòng thử lại";
                        }
                        else
                        {
                            thoiGian = ConvertTo.Int(rowDeThi["THOIGIAN"]);
                            soCau = ConvertTo.Int(rowDeThi["SOCAU"]);

                            //Cập nhật giờ bắt đầu, trạmg thái đang làm
                            cmd.CommandText = "SELECT GIOBATDAU FROM TDOTTUYENDUNGTHIMAYCT WHERE ID = @TDOTTUYENDUNGTHIMAYCTID";
                            dangLam = db.GetFirstField(cmd) != DBNull.Value;

                            //Lấy câu hỏi của đề thi
                            if (dangLam)
                            {
                                fromDate = ConvertTo.Date(rowDotThi["GIOBATDAU"]);
                                //Lấy danh sách câu hỏi và câu trả lời
                                cmd.CommandText = @"SELECT DCAUHOI.ID, DCAUHOI.NAME, COALESCE(DAODAPAN, 0) AS DAODAPAN, COALESCE(NHIEUCAUTRALOI, 0) AS NHIEUCAUTRALOI FROM TBAITHI
                                INNER JOIN DCAUHOI ON TBAITHI.DCAUHOIID = DCAUHOI.ID WHERE TDOTTUYENDUNGTHIMAYCTID = @TDOTTUYENDUNGTHIMAYCTID AND DDETHIID = @DDETHIID ORDER BY TBAITHI.THUTU ASC";
                                dtCauHoi = db.GetTable(cmd);
                                cmd.CommandText = @"SELECT DCAUHOICHITIET.ID, TBAITHICHITIET.DCAUHOIID, NOIDUNG, COALESCE(TBAITHICHITIET.LADAPANDUNG, 0) AS LADAPANDUNG, TBAITHICHITIET.LACAUTRALOI, TBAITHICHITIET.THUTU FROM DCAUHOICHITIET
                                INNER JOIN TBAITHICHITIET ON DCAUHOICHITIET.ID = TBAITHICHITIET.DCAUHOICHITIETID WHERE TDOTTUYENDUNGTHIMAYCTID = @TDOTTUYENDUNGTHIMAYCTID AND DDETHIID = @DDETHIID ORDER BY THUTU ASC";
                                dtDapAn = db.GetTable(cmd);
                            }
                            else
                            {
                                fromDate = db.DbDateTime;

                                //chuẩn bị tham số
                                FbCommand cmdChiTiet = db.GetCommand(@"INSERT INTO TBAITHI(ID, STATUS, TIMECREATED, USERCREATEDID, TDOTTUYENDUNGTHIMAYCTID, THUTU, DCAUHOIID, DDETHIID, DVONGTUYENDUNGID)
                                VALUES (LOWER(NEWID()), 30, GETDATE(), 'PM_DAOTAO_TUYENDUNG', @TDOTTUYENDUNGTHIMAYCTID, @THUTU, @DCAUHOIID, @DDETHIID, @DVONGTUYENDUNGID)");
                                cmdChiTiet.Parameters.Add("@THUTU", FbDbType.Integer).Value = 0;
                                cmdChiTiet.Parameters.Add("@DCAUHOIID", FbDbType.VarChar).Value = "";
                                cmdChiTiet.Parameters.Add("@DDETHIID", FbDbType.VarChar).Value = DDETHIID;
                                cmdChiTiet.Parameters.Add("@TDOTTUYENDUNGTHIMAYCTID", FbDbType.VarChar).Value = "";
                                cmdChiTiet.Parameters.Add("@DVONGTUYENDUNGID", FbDbType.VarChar).Value = DVONGTUYENDUNGID;

                                //Tổng hợp câu hỏi của đề
                                CachChonCauHoi cachChon = (CachChonCauHoi)ConvertTo.Int(rowDeThi["CACHCHON"]);
                                if (cachChon == CachChonCauHoi.TuChon)
                                {
                                    //tự chọn
                                    bool daoTratTuCauHoi = ConvertTo.Int(rowDeThi["DAOTRATTUCAUHOI"]) > 0;
                                    //Lấy danh sách câu hỏi
                                    cmd.CommandText = @"SELECT DCAUHOI.ID, DCAUHOI.NAME, COALESCE(DAODAPAN, 0) AS DAODAPAN, COALESCE(NHIEUCAUTRALOI, 0) AS NHIEUCAUTRALOI FROM DDETHICHITIET
                                    INNER JOIN DCAUHOI ON DDETHICHITIET.DCAUHOIID = DCAUHOI.ID WHERE DDETHIID = @DDETHIID";
                                    DataTable dtTemp = db.GetTable(cmd);

                                    List<int> lstIndex = new List<int>();
                                    if (daoTratTuCauHoi)
                                    {
                                        Random rd = new Random();
                                        while (lstIndex.Count < dtTemp.Rows.Count)
                                        {
                                            int index = rd.Next(0, dtTemp.Rows.Count);
                                            if (!lstIndex.Contains(index))
                                            {
                                                lstIndex.Add(index);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (int i = 0; i < dtTemp.Rows.Count; i++)
                                        {
                                            lstIndex.Add(i);
                                        }
                                    }

                                    dtCauHoi = dtTemp.Clone();
                                    int demTemp = 0;
                                    foreach (int index in lstIndex)
                                    {
                                        DataRow row = dtCauHoi.NewRow();
                                        copyData(row, dtTemp.Rows[index]);
                                        dtCauHoi.Rows.Add(row);
                                        //Lưu chi tiết, thứ tự câu hỏi
                                        cmdChiTiet.Parameters["@THUTU"].Value = demTemp;
                                        cmdChiTiet.Parameters["@DCAUHOIID"].Value = row["ID"];
                                        cmdChiTiet.Parameters["@TDOTTUYENDUNGTHIMAYCTID"].Value = rowDotThi["ID"];
                                        db.ExecSql(cmdChiTiet);
                                        demTemp++;
                                    }

                                    //Lấy danh sách câu trả lời
                                    cmd.CommandText = @"SELECT ID, DCAUHOIID, NOIDUNG, COALESCE(LADAPANDUNG, 0) AS LADAPANDUNG, 0 AS LACAUTRALOI, 0 AS THUTU FROM DCAUHOICHITIET WHERE EXISTS 
                                    (SELECT * FROM DCAUHOI INNER JOIN DDETHICHITIET ON DCAUHOI.ID = DDETHICHITIET.DCAUHOIID
                                    WHERE DCAUHOIID = DCAUHOI.ID AND DDETHIID = @DDETHIID)";
                                    dtTemp = db.GetTable(cmd);

                                    ImportDataDapAn(db, cmdChiTiet, dtCauHoi, ref dtDapAn, dtTemp, rowDotThi);
                                }
                                else
                                {
                                    Random rd = new Random();
                                    //tự động
                                    cmd.CommandText = "SELECT DNHOMCAUHOIID FROM DDETHI WHERE ID = @DDETHIID";
                                    string DNHOMCAUHOIID = db.GetFirstFieldString(cmd);
                                    List<string> lstCauHoi = new List<string>();
                                    cmd.CommandText = "SELECT DLOAICAUHOIID, TIILEPHANTRAM FROM DDETHICHITIET WHERE DDETHIID = @DDETHIID ORDER BY TIILEPHANTRAM ASC";
                                    DataTable dtTiLe = db.GetTable(cmd);
                                    cmd.Parameters.Add("@DNHOMCAUHOIID", FbDbType.VarChar).Value = DNHOMCAUHOIID;
                                    cmd.CommandText = "SELECT ID, DLOAICAUHOIID FROM DCAUHOI WHERE STATUS = 30 AND DNHOMCAUHOIID = @DNHOMCAUHOIID";
                                    DataTable dtTemp = db.GetTable(cmd);
                                    int count = 0;
                                    //b1.Lọc câu hỏi theo tỉ lệ
                                    dtTiLe.DefaultView.Sort = "TIILEPHANTRAM";
                                    dtTiLe = dtTiLe.DefaultView.ToTable();
                                    for (int i = 0; i < dtTiLe.Rows.Count; i++)
                                    {
                                        DataRow rLoaiCauHoi = dtTiLe.Rows[i];
                                        decimal tile = ConvertTo.Decimal(rLoaiCauHoi["TIILEPHANTRAM"]);
                                        if (tile > 0)
                                        {
                                            string DLOAICAUHOIID = ConvertTo.String(rLoaiCauHoi["DLOAICAUHOIID"]);
                                            int dem = (int)Math.Floor(soCau * tile / 100);
                                            if (dem == 0) dem = 1;
                                            if (i == dtTiLe.Rows.Count - 1) dem = soCau - count;
                                            DataRow[] arrTemp = dtTemp.Select("DLOAICAUHOIID='" + DLOAICAUHOIID + "'");
                                            if (arrTemp.Length > 0)
                                            {
                                                dem = arrTemp.Length < dem ? arrTemp.Length : dem;
                                                int demAdd = 0;
                                                string DCAUHOIID = "";
                                                do
                                                {
                                                    int stt = rd.Next(0, arrTemp.Length);
                                                    DCAUHOIID = ConvertTo.String(arrTemp[stt]["ID"]);
                                                    if (!lstCauHoi.Contains(DCAUHOIID))
                                                    {
                                                        demAdd++;
                                                        lstCauHoi.Add(DCAUHOIID);
                                                    }
                                                }
                                                while (demAdd < dem);
                                            }
                                            count += dem;
                                        }
                                    }
                                    //b2.Lấy nội dung câu hỏi
                                    string whereCauHoi = "";
                                    foreach (string cauHoiId in lstCauHoi)
                                    {
                                        if (whereCauHoi.Length > 0) whereCauHoi += ",";
                                        whereCauHoi += "'"+ cauHoiId +"'";

                                        //Lưu chi tiết, thứ tự câu hỏi
                                        cmdChiTiet.Parameters["@THUTU"].Value = lstCauHoi.IndexOf(cauHoiId);
                                        cmdChiTiet.Parameters["@DCAUHOIID"].Value = cauHoiId;
                                        cmdChiTiet.Parameters["@TDOTTUYENDUNGTHIMAYCTID"].Value = rowDotThi["ID"];
                                        db.ExecSql(cmdChiTiet);
                                    }
                                    cmd.CommandText = @"SELECT DCAUHOI.ID, DCAUHOI.NAME, COALESCE(DAODAPAN, 0) AS DAODAPAN, COALESCE(NHIEUCAUTRALOI, 0) AS NHIEUCAUTRALOI FROM TBAITHI
                                    INNER JOIN DCAUHOI ON TBAITHI.DCAUHOIID = DCAUHOI.ID WHERE TDOTTUYENDUNGTHIMAYCTID = @TDOTTUYENDUNGTHIMAYCTID AND DDETHIID = @DDETHIID ORDER BY TBAITHI.THUTU ASC";
                                    dtCauHoi = db.GetTable(cmd);

                                    //b3.Lấy nội dung câu trả lời
                                    cmd.CommandText = "SELECT ID, DCAUHOIID, NOIDUNG, COALESCE(LADAPANDUNG, 0) AS LADAPANDUNG, 0 AS LACAUTRALOI, 0 AS THUTU FROM DCAUHOICHITIET WHERE DCAUHOIID IN (" + whereCauHoi + ")";
                                    dtTemp = db.GetTable(cmd);
                                    ImportDataDapAn(db, cmdChiTiet, dtCauHoi, ref dtDapAn, dtTemp, rowDotThi);
                                }

                                cmd.CommandText = "UPDATE TDOTTUYENDUNGTHIMAYCT SET GIOBATDAU = @GIOBATDAU WHERE ID = @ID";
                                cmd.Parameters.Add("@GIOBATDAU", FbDbType.Date).Value = db.DbDateTime;
                                db.ExecSql(cmd);
                            }
                        }

                        ViewBag.id = id;
                        ViewBag.deThiId = DDETHIID;
                        ViewBag.thoiGian = thoiGian;
                        ViewBag.tgConLai = (int)(dangLam ? (fromDate.AddMinutes(thoiGian) - db.DbDateTime).TotalSeconds : thoiGian * 60);
                        ViewBag.soCau = soCau;
                        ViewBag.CauHoi = dtCauHoi;
                        ViewBag.DapAn = dtDapAn;
                    }
                    else
                    {
                        error = "Có lỗi trong quá trình tải đề thi, vui lòng thử lại";
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
            return View();
        }

        private void ImportDataDapAn(Database db, FbCommand cmdChiTiet, DataTable dtCauHoi, ref DataTable dtDapAn, DataTable dtTemp, DataRow rowDotThi)
        {
            cmdChiTiet.CommandText = @"INSERT INTO TBAITHICHITIET(ID, STATUS, TIMECREATED, USERCREATEDID, TDOTTUYENDUNGTHIMAYCTID, THUTU, DCAUHOIID, DCAUHOICHITIETID, LACAUTRALOI, LADAPANDUNG, DDETHIID)
            VALUES (LOWER(NEWID()), 30, GETDATE(), 'PM_DAOTAO_TUYENDUNG', @TDOTTUYENDUNGTHIMAYCTID, @THUTU, @DCAUHOIID, @DCAUHOICHITIETID, @LACAUTRALOI, @LADAPANDUNG, @DDETHIID)";
            cmdChiTiet.Parameters.Add("@DCAUHOICHITIETID", FbDbType.VarChar).Value = "";
            cmdChiTiet.Parameters.Add("@LACAUTRALOI", FbDbType.VarChar).Value = 0;
            cmdChiTiet.Parameters.Add("@LADAPANDUNG", FbDbType.VarChar).Value = 0;
            foreach (DataRow rowCauHoi in dtCauHoi.Rows)
            {
                bool daoDapAn = ConvertTo.Int(rowCauHoi["DAODAPAN"]) > 0;
                DataRow[] rows = dtTemp.Select("DCAUHOIID='" + ConvertTo.String(rowCauHoi["ID"]) + "'");
                List<int> lstIndex = new List<int>();
                if (daoDapAn)
                {
                    Random rd = new Random();
                    while (lstIndex.Count < rows.Length)
                    {
                        int index = rd.Next(0, rows.Length);
                        if (!lstIndex.Contains(index)) lstIndex.Add(index);
                    }
                }
                else
                {
                    for (int i = 0; i < rows.Length; i++)
                    {
                        lstIndex.Add(i);
                    }
                }

                if (dtDapAn == null) dtDapAn = dtTemp.Clone();
                int demTemp = 0;
                foreach (int index in lstIndex)
                {
                    DataRow row = dtDapAn.NewRow();
                    row["THUTU"] = demTemp;
                    copyData(row, rows[index]);
                    dtDapAn.Rows.Add(row);
                    //Lưu chi tiết, thứ tự câu trả lời
                    cmdChiTiet.Parameters["@TDOTTUYENDUNGTHIMAYCTID"].Value = rowDotThi["ID"];
                    cmdChiTiet.Parameters["@THUTU"].Value = demTemp;
                    cmdChiTiet.Parameters["@DCAUHOIID"].Value = row["DCAUHOIID"];
                    cmdChiTiet.Parameters["@DCAUHOICHITIETID"].Value = row["ID"];
                    cmdChiTiet.Parameters["@LADAPANDUNG"].Value = row["LADAPANDUNG"];
                    db.ExecSql(cmdChiTiet);
                    demTemp++;
                }
            }
        }

        private void copyData(DataRow row, DataRow dataRow)
        {
            foreach (DataColumn col in row.Table.Columns)
            {
                if (dataRow.Table.Columns.Contains(col.ColumnName))
                {
                    row[col.ColumnName] = dataRow[col.ColumnName];
                }
            }
        }

        public ActionResult LuuCauTraLoi(string id, string deThiId, string cauHoiId, string traLoiId, int active)
        {
            string error = "";
            Database db = DatabaseUtils.GetDatabase();
            db.TryConnect(out error);

            if (error.Length == 0)
            {
                try
                {
                    FbCommand cmd = db.GetCommand("");
                    cmd.Parameters.Add("@TDOTTUYENDUNGTHIMAYCTID", FbDbType.VarChar).Value = id;
                    cmd.Parameters.Add("@DCAUHOIID", FbDbType.VarChar).Value = cauHoiId;
                    cmd.Parameters.Add("@DCAUHOICHITIETID", FbDbType.VarChar).Value = traLoiId;
                    cmd.Parameters.Add("@DDETHIID", FbDbType.VarChar).Value = deThiId;
                    cmd.Parameters.Add("@LACAUTRALOI", FbDbType.VarChar).Value = active;

                    //b1.Kiểm tra câu hỏi là nhiều hay 1 câu trả 
                    cmd.CommandText = "SELECT COALESCE(NHIEUCAUTRALOI, 0) FROM DCAUHOI WHERE ID = @DCAUHOIID";
                    bool nhieuCauTraLoi = db.GetFirstFieldInt(cmd) > 0;
                    if (!nhieuCauTraLoi)
                    {
                        cmd.CommandText = "UPDATE TBAITHICHITIET SET LACAUTRALOI = 0 WHERE TDOTTUYENDUNGTHIMAYCTID = @TDOTTUYENDUNGTHIMAYCTID AND DCAUHOIID = @DCAUHOIID AND DDETHIID = @DDETHIID";
                        db.ExecSql(cmd);
                    }

                    if (nhieuCauTraLoi || (!nhieuCauTraLoi && active > 0))
                    {
                        //b2.Cập nhật dữ liệu
                        cmd.CommandText = "UPDATE TBAITHICHITIET SET LACAUTRALOI = @LACAUTRALOI WHERE TDOTTUYENDUNGTHIMAYCTID = @TDOTTUYENDUNGTHIMAYCTID AND DCAUHOIID = @DCAUHOIID AND DCAUHOICHITIETID = @DCAUHOICHITIETID AND DDETHIID = @DDETHIID";
                        db.ExecSql(cmd);
                    }
                }
                catch (Exception ex)
                {
                    error = "Có lỗi trong quá trình lưu dữ liệu, vui lòng thử lại: " + ex.Message;
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
            return Content(error);
        }

        public ActionResult RenderCardBodyBailam(DataTable dtCauHoi, DataTable dtDapAn, bool ketThuc, string error)
        {
            ViewBag.CauHoi = dtCauHoi;
            ViewBag.DapAn = dtDapAn;
            ViewBag.KetThuc = ketThuc;
            ViewBag.error = error;
            return PartialView();
        }
        public ActionResult KetQua(string id)
        {
            string error = "";
            Database db = DatabaseUtils.GetDatabase();
            db.TryConnect(out error);
            DataRow rKq = null;

            if (error.Length == 0)
            {
                try
                {
                    //lấy kết quả
                    FbCommand cmd = db.GetCommand("");
                    cmd.Parameters.Add("@TDOTTUYENDUNGTHIMAYCTID", FbDbType.VarChar).Value = id;

                    cmd.CommandText = @"SELECT DDETHI.NAME, COALESCE(KETTHUC, 0) AS KETTHUC, DDETHIID, DIEMTOIDA, DIEM, SOCAUTRALOIDUNG,
                    (
                        SELECT COUNT(DISTINCT DCAUHOIID) FROM TBAITHICHITIET WHERE TDOTTUYENDUNGTHIMAYCTID = TDOTTUYENDUNGTHIMAYCT.ID AND COALESCE(LACAUTRALOI, 0) > 0
                    ) AS SOCAUDALAM, SOCAU, LOAIDIEM, TDOTTUYENDUNGID, TDOTTUYENDUNGCHITIETID, DVONGTUYENDUNGID
                    FROM TDOTTUYENDUNGTHIMAYCT INNER JOIN DDETHI ON DDETHIID = DDETHI.ID WHERE TDOTTUYENDUNGTHIMAYCT.ID = @TDOTTUYENDUNGTHIMAYCTID";
                    rKq = db.GetFirstRow(cmd);
                    bool ketThuc = ConvertTo.Int(rKq["KETTHUC"]) > 0;
                    string DDETHIID = ConvertTo.String(rKq["DDETHIID"]);
                    decimal diemToiDa = ConvertTo.Decimal(rKq["DIEMTOIDA"]);
                    decimal loaiDiem = ConvertTo.Decimal(rKq["LOAIDIEM"]);
                    string TDOTTUYENDUNGID = ConvertTo.String(rKq["TDOTTUYENDUNGID"]);
                    string TDOTTUYENDUNGCHITIETID = ConvertTo.String(rKq["TDOTTUYENDUNGCHITIETID"]);
                    string DVONGTUYENDUNGID = ConvertTo.String(rKq["DVONGTUYENDUNGID"]);

                    cmd.Parameters.Add("@DDETHIID", FbDbType.VarChar).Value = DDETHIID;
                    //View ket qua
                    cmd.CommandText = @"SELECT DCAUHOI.ID, DCAUHOI.NAME, COALESCE(DAODAPAN, 0) AS DAODAPAN, COALESCE(NHIEUCAUTRALOI, 0) AS NHIEUCAUTRALOI
                    FROM TBAITHI INNER JOIN DCAUHOI ON TBAITHI.DCAUHOIID = DCAUHOI.ID
                    WHERE TDOTTUYENDUNGTHIMAYCTID = @TDOTTUYENDUNGTHIMAYCTID AND DDETHIID = @DDETHIID ORDER BY TBAITHI.THUTU ASC";
                    DataTable dtCauHoi = db.GetTable(cmd);
                    cmd.CommandText = @"SELECT DCAUHOICHITIET.ID, TBAITHICHITIET.DCAUHOIID, NOIDUNG, COALESCE(TBAITHICHITIET.LADAPANDUNG, 0) AS LADAPANDUNG, TBAITHICHITIET.LACAUTRALOI, TBAITHICHITIET.THUTU FROM DCAUHOICHITIET
                    INNER JOIN TBAITHICHITIET ON DCAUHOICHITIET.ID = TBAITHICHITIET.DCAUHOICHITIETID WHERE TDOTTUYENDUNGTHIMAYCTID = @TDOTTUYENDUNGTHIMAYCTID AND DDETHIID = @DDETHIID ORDER BY THUTU ASC";
                    DataTable dtDapAn = db.GetTable(cmd);

                    if (!ketThuc)
                    {
                        //Tính toán điểm
                        decimal soCauDung = 0;
                        foreach (DataRow rCauHoi in dtCauHoi.Rows)
                        {
                            string DCAUHOIID = ConvertTo.String(rCauHoi["ID"]);
                            DataRow[] rows = dtDapAn.Select("DCAUHOIID='"+DCAUHOIID+"'");
                            bool dung = true;
                            foreach (DataRow rTraLoi in rows)
                            {
                                bool laDapAnDung = ConvertTo.Int(rTraLoi["LADAPANDUNG"]) > 0;
                                bool laCauTraLoi = ConvertTo.Int(rTraLoi["LACAUTRALOI"]) > 0;
                                if (laDapAnDung && !laCauTraLoi || laCauTraLoi && !laDapAnDung) dung = false;
                            }
                            if (dung) soCauDung++;
                        }

                        decimal diem = 0;
                        //tính toán điểm
                        diem = diemToiDa * (soCauDung / dtCauHoi.Rows.Count);

                        //cập nhật trạng thái kết thúc
                        cmd.Parameters.Add("@DIEM", FbDbType.Decimal).Value = diem;
                        cmd.Parameters.Add("@SOCAUTRALOIDUNG", FbDbType.Decimal).Value = soCauDung;
                        cmd.CommandText = "UPDATE TDOTTUYENDUNGTHIMAYCT SET KETTHUC = 30, SOCAUTRALOIDUNG = @SOCAUTRALOIDUNG, DIEM = @DIEM WHERE ID = @TDOTTUYENDUNGTHIMAYCTID AND COALESCE(KETTHUC, 0) = 0";
                        db.ExecSql(cmd);

                        //Lưu điểm vào đợt tuyển dụng
                        cmd.CommandText = @"SELECT PARENTID FROM DVONGTUYENDUNG WHERE ID = (SELECT TDOTTUYENDUNGCHITIET.DVONGTUYENDUNGID FROM TDOTTUYENDUNGCHITIET
                        INNER JOIN TDOTTUYENDUNGTHIMAYCT ON TDOTTUYENDUNGCHITIETID = TDOTTUYENDUNGCHITIET.ID WHERE TDOTTUYENDUNGTHIMAYCT.ID = @TDOTTUYENDUNGTHIMAYCTID)";
                        string PARENTID = db.GetFirstFieldString(cmd);

                        DataRow rUser = (DataRow)SessionManager.GetObject(Contants.USER_SESSION);
                        string DUNGVIENID = ConvertTo.String(rUser["ID"]);
                        string fieldDiem = "DIEM" + (loaiDiem == 0 ? "" : (loaiDiem + 1).ToString());
                        cmd.Parameters.Add("@PARENTID", FbDbType.VarChar).Value = PARENTID;
                        cmd.Parameters.Add("@DUNGVIENID", FbDbType.VarChar).Value = DUNGVIENID;
                        cmd.Parameters.Add("@TDOTTUYENDUNGID", FbDbType.VarChar).Value = TDOTTUYENDUNGID;

                        cmd.CommandText = @"UPDATE TDOTTUYENDUNGCHITIET SET TDOTTUYENDUNGCHITIET."+fieldDiem+@" = @DIEM
                        FROM TDOTTUYENDUNGCHITIET
WHERE TDOTTUYENDUNGCHITIET.DUNGVIENID = @DUNGVIENID AND TDOTTUYENDUNGCHITIET.TDOTTUYENDUNGID = @TDOTTUYENDUNGID
AND EXISTS (SELECT * FROM DVONGTUYENDUNG WHERE ID = TDOTTUYENDUNGCHITIET.DVONGTUYENDUNGID AND @PARENTID = DVONGTUYENDUNG.PARENTID)";
                        db.ExecSql(cmd);

                        rKq["SOCAUTRALOIDUNG"] = soCauDung;
                        rKq["DIEM"] = Math.Round(diem, 2);

                        //kiểm tra chuyển đổi trạng thái nhân viên
                        //nếu đã làm hết tất cả các bài kiểm tra thì mới được đổi

                        //tính xem vòng này có mấy bài
                        bool coMonThi2 = false, coMonThi3 = false;
                        decimal diemDatMon1 = 0, diemDatMon2 = 0, diemDatMon3 = 0;
                        decimal tongPhaiLam = LaySLPhaiLam(db, DVONGTUYENDUNGID, ref diemDatMon1, ref coMonThi2, ref diemDatMon2, ref coMonThi3, ref diemDatMon3);
                        //lấy số lượng bài đã làm
                        cmd.Parameters.Add("@TDOTTUYENDUNGCHITIETID", FbDbType.VarChar).Value = TDOTTUYENDUNGCHITIETID;
                        cmd.Parameters.Add("@DVONGTUYENDUNGID", FbDbType.VarChar).Value = DVONGTUYENDUNGID;
                        cmd.CommandText = "SELECT * FROM TDOTTUYENDUNGTHIMAYCT WHERE TDOTTUYENDUNGCHITIETID = @TDOTTUYENDUNGCHITIETID AND DVONGTUYENDUNGID = @DVONGTUYENDUNGID AND KETTHUC = 30";
                        DataTable dtDaLam = db.GetTable(cmd);
                        if (tongPhaiLam == dtDaLam.Rows.Count)
                        {
                            //tính toán chuyển trạng thái ứng viên
                            bool truot = false;
                            foreach (DataRow row in dtDaLam.Rows)
                            {
                                if (!truot)
                                {
                                    decimal diemTemp = ConvertTo.Decimal(row["DIEM"]);
                                    int loaiTemp = ConvertTo.Int(row["LOAIDIEM"]);
                                    if (loaiTemp == 0)
                                    {
                                        if (diemTemp < diemDatMon1) truot = true;
                                    }
                                    if (loaiTemp == 1 && coMonThi2)
                                    {
                                        if (diemTemp < diemDatMon2) truot = true;
                                    }
                                    if (loaiTemp == 2 && coMonThi3)
                                    {
                                        if (diemTemp < diemDatMon3) truot = true;
                                    }
                                }
                            }

                            //Cập nhật trạng thái
                            cmd.CommandText = "SELECT BUOCTIEPTHEOID FROM TBUOCKETIEPVONGTD WHERE DVONGTUYENDUNGID = '"+PARENTID+"' AND " + (truot ? "BUOCTIEPTHEONEUKHONGDAT" : "BUOCTIEPTHEONEUDAT") + "=30";
                            string buocTiepTheoId = db.GetFirstFieldString(cmd);
                            if (buocTiepTheoId.Length > 0)
                            {
                                //cập nhật dữ liệu
                                db.ExecSql("UPDATE TDOTTUYENDUNGCHITIET SET LANCUOI = 0 WHERE DUNGVIENID = '" + DUNGVIENID + "' AND TDOTTUYENDUNGID = '" + TDOTTUYENDUNGID + "'");
                                //insert new row
                                cmd.CommandText = @"INSERT INTO TDOTTUYENDUNGCHITIET(ID, STATUS, TIMECREATED, USERCREATEDID, TDOTTUYENDUNGID, DUNGVIENID, DMAUEMAILID, TSUKIENTUYENDUNGID, DAGUIEMAIL, DIEM, DIEM2, DIEM3, LANCUOI, DVONGTUYENDUNGID)
                                SELECT LOWER(NEWID()), 30, GETDATE(), 'PM_DaoTao_TuyenDung', TDOTTUYENDUNGID, DUNGVIENID, DMAUEMAILID, TSUKIENTUYENDUNGID, DAGUIEMAIL, DIEM, DIEM2, DIEM3, 30, '"+ buocTiepTheoId + @"'
                                FROM TDOTTUYENDUNGCHITIET WHERE ID = @TDOTTUYENDUNGCHITIETID";
                                db.ExecSql(cmd);
                            }
                        }
                    }

                    ViewBag.CauHoi = dtCauHoi;
                    ViewBag.DapAn = dtDapAn;
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

            if (rKq == null) error = "Không tìm thấy dữ liệu yêu cầu";
            if (error.Length > 0) ViewBag.error = error;
            return View(rKq);
        }

        private decimal LaySLPhaiLam(Database db, string DVONGTUYENDUNGID, ref decimal diemDatMon1, ref bool coMonThi2, ref decimal diemDatMon2, ref bool coMonThi3, ref decimal diemDatMon3)
        {
            decimal dem = 1;
            DataTable dtVongTuyenDung = db.GetTable("SELECT ID, PARENTID, COTHIMON1, TENMONTHI1, DIEMDAT, COTHIMON2, TENMONTHI2, DIEMDATMON2, COTHIMON3, TENMONTHI3, DIEMDATMON3 FROM DVONGTUYENDUNG");
            //đệ quy lấy thằng cha
            DataRow row = dtVongTuyenDung.Select("ID='" + DVONGTUYENDUNGID + "'")[0];
            while (ConvertTo.String(row["PARENTID"]).Length > 0)
            {
                row = dtVongTuyenDung.Select("ID='" + ConvertTo.String(row["PARENTID"]) + "'")[0];
            }

            diemDatMon1 = ConvertTo.Decimal(row["DIEMDAT"]);
            diemDatMon2 = ConvertTo.Decimal(row["DIEMDATMON2"]);
            diemDatMon3 = ConvertTo.Decimal(row["DIEMDATMON3"]);

            if (ConvertTo.Int(row["COTHIMON2"]) > 0)
            {
                dem++;
                coMonThi2 = true;
            }
            if (ConvertTo.Int(row["COTHIMON3"]) > 0)
            {
                dem++;
                coMonThi3 = true;
            }
            return dem;
        }
    }

    public enum CachChonCauHoi
    {
        TuChon = 0,
        TuDong = 1
    }
}
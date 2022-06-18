using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TVS_DT_TD.Controllers.Filter;

namespace TVS_DT_TD.Controllers
{
    [AuthFilter]
    public class DanhGiaController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
using System.Web;
using System.Web.Optimization;

namespace TVS_DT_TD
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            //login
            bundles.Add(new StyleBundle("~/Login/css").Include("~/Content/login/site.css"));
            //home
            bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/bootstrap.min.css", "~/Content/site.css", "~/Content/font-awesome/css/font-awesome.min.css"));
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include("~/Scripts/jquery-3.2.1.slim.js", "~/Scripts/jquery-3.6.0.js"));
            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include("~/Scripts/popper.min.js", "~/Scripts/bootstrap.min.js"));
        }
    }
}
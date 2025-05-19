using System.Web;
using System.Web.Optimization;

namespace ReqIFBridge
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            // The Kendo JavaScript bundle
            string[] bundleKendoScript = new string[]
            {
                "~/Scripts/Kendo/kendo.all.min.js",  // or kendo.all.min.js if you want to use Kendo UI Web and Kendo UI DataViz
               
                "~/Scripts/Kendo/kendo.aspnetmvc.min.js",
                "~/Scripts/Kendo/kendo.mobile.switch.min.js"


            };

            bundles.Add(new ScriptBundle("~/bundles/Kendo/KendoScripts")
                .Include(bundleKendoScript));

            bundles.Add(new ScriptBundle("~/bundles/vsssdk").Include(
                "~/Scripts/vsssdk/VSS.SDK.js"));


            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js"));

            // The Kendo Style bundle

            string[] bundleKendoStyle = new string[]
           {
                "~/Content/Kendo/kendo.common.min.css",
                "~/Content/Kendo/kendo.default.min.css",
                "~/Content/Kendo/kendo.default.mobile.min.css",
                "~/Content/Kendo/kendo.rtl.min.css",
                "~/Content/Kendo/kendo.metro.min.css",
                "~/Content/Kendo/kendo.dataviz.metro.min.css"
           };

            // The Kendo Style bundle
            bundles.Add(new StyleBundle("~/Content/Kendo/bundleKendoStyle")
                .Include(bundleKendoStyle));

            bundles.Add(new StyleBundle("~/Content/Kendo/kendo.common")
                .Include("~/Content/Kendo/kendo.common.min.css"));

            bundles.Add(new StyleBundle("~/Content/Kendo/kendo.rtl")
                .Include("~/Content/Kendo/kendo.rtl.min.css"));

            bundles.Add(new StyleBundle("~/Content/Kendo/kendo.metro")
                .Include("~/Content/Kendo/kendo.metro.min.css"));

            bundles.Add(new StyleBundle("~/Content/Kendo/kendo.dataviz.metro")
                .Include("~/Content/Kendo/kendo.dataviz.metro.min.css"));

            // The Application Style bundle
            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));

#if !DEBUG
            BundleTable.EnableOptimizations = true;
#endif
        }
    }
}

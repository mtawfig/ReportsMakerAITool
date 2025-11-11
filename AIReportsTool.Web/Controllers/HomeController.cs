using Microsoft.AspNetCore.Mvc;

namespace AIReportsTool.Web.Controllers
{
    /// <summary>
    /// Home controller handling the main landing page
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// GET: /Home/Index or /
        /// Displays the main application interface
        /// </summary>
        public IActionResult Index()
        {
            // Read language from cookie, default to "en"
            var language = Request.Cookies["Language"] ?? "en";
            ViewBag.Language = language;
            ViewBag.Direction = language == "ar" ? "rtl" : "ltr";

            return View();
        }
    }
}

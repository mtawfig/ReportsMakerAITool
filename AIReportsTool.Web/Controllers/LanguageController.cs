using Microsoft.AspNetCore.Mvc;

namespace AIReportsTool.Web.Controllers
{
    /// <summary>
    /// Controller handling language switching functionality
    /// Sets cookie and redirects to maintain user language preference
    /// </summary>
    public class LanguageController : Controller
    {
        /// <summary>
        /// POST: /Language/Switch
        /// Switches the application language and sets cookie
        /// </summary>
        //[HttpPost]
        //public IActionResult Switch(string lang)
        //{
        //    if (string.IsNullOrWhiteSpace(lang))
        //    {
        //        lang = "en";
        //    }

        //    // Validate language (only allow "en" or "ar")
        //    lang = lang.ToLower();
        //    if (lang != "en" && lang != "ar")
        //    {
        //        lang = "en";
        //    }

        //    // Set cookie for 30 days
        //    Response.Cookies.Append("Language", lang, new CookieOptions
        //    {
        //        Expires = DateTimeOffset.UtcNow.AddDays(30),
        //        HttpOnly = true,
        //        IsEssential = true
        //    });

        //    // Redirect back to home
        //    return RedirectToAction("Index", "Home");
        //}

        /// <summary>
        /// GET: /Language/Switch
        /// Alternative GET endpoint for language switching
        /// </summary>
        [HttpGet]
        public IActionResult Switch(string? lang = "en")
        {
            if (string.IsNullOrWhiteSpace(lang))
            {
                lang = "en";
            }

            // Validate language (only allow "en" or "ar")
            lang = lang.ToLower();
            if (lang != "en" && lang != "ar")
            {
                lang = "en";
            }

            // Set cookie for 30 days
            Response.Cookies.Append("Language", lang, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                IsEssential = true
            });

            // Redirect back to home
            return RedirectToAction("Index", "Home");
        }
    }
}

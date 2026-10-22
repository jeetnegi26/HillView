using DTOs;
using Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace HillView.sln.Controllers
{
    public class HomeController : Controller
    {
        private readonly LogErrorService _logErrorService;
        private readonly UIPageService _uiPageService;
        private readonly WebInfoService _webInfoService;
        private readonly ContactService _contactService;

        // Read from <appSettings> key "Index" in Web.config
        private static readonly string HomePageId =
            ConfigurationManager.AppSettings["Index"];

        private static readonly string AboutPageId =
            ConfigurationManager.AppSettings["About"];

        private static readonly string ContactPageId =
            ConfigurationManager.AppSettings["Contact"];

        public HomeController(LogErrorService logErrorService,  UIPageService uiPageService, 
            WebInfoService webInfoService, ContactService contactService)
        {
            _logErrorService = logErrorService;
            _uiPageService = uiPageService;
            _webInfoService = webInfoService;
            _contactService = contactService;
        }
        public async Task<ActionResult> Index()
        {
            try
            {
                if (!Guid.TryParse(HomePageId, out var pageId))
                {
                    throw new InvalidOperationException("AppSettings key 'Index' is missing or is not a valid GUID.");
                }

                var model = await _uiPageService.GetPageContentAsync(pageId, Request.UserHostAddress);

                if (model == null)
                {
                    return HttpNotFound();
                }

                return View(model);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public ActionResult advantages(List<DTOs.UISectionVM> sections)
        {
            return PartialView("~/Views/Home/partial/_advantages.cshtml", sections);
        }
        public ActionResult milestones(List<DTOs.UISectionVM> sections)
        {
            return PartialView("~/Views/Home/partial/_milestones.cshtml", sections);
        }
        public ActionResult aitalent(List<DTOs.UISectionVM> sections)
        {
            return PartialView("~/Views/Home/partial/_aitalent.cshtml", sections);
        }

        [Route("about")]
        public async Task<ActionResult> About()
        {
            try
            {
                if (!Guid.TryParse(AboutPageId, out var pageId))
                {
                    throw new InvalidOperationException("AppSettings key 'About' is missing or is not a valid GUID.");
                }

                var model = await _uiPageService.GetPageContentAsync(pageId, Request.UserHostAddress);

                if (model == null)
                {
                    return HttpNotFound();
                }

                return View(model);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [Route("contact-us")]
        public async Task<ActionResult> Contact()
        {
            try
            {
                if (!Guid.TryParse(ContactPageId, out var pageId))
                {
                    throw new InvalidOperationException("AppSettings key 'Index' is missing or is not a valid GUID.");
                }

                var model = await _uiPageService.GetPageContentAsync(pageId, Request.UserHostAddress);

                if (model == null)
                {
                    return HttpNotFound();
                }

                return View(model);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SubmitContact(ContactVM model)
        {
            const string thankYou =
                "Thank you! Your message has been sent. We will get back to you shortly.";

            try
            {
                if (!ModelState.IsValid)
                {
                    string error = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                        ?? "Please check the details you entered.";

                    return Json(new { success = false, message = error });
                }

                // Honeypot: a bot filled the hidden field. Pretend it worked, save nothing.
                if (!string.IsNullOrWhiteSpace(model.Website))
                {
                    return Json(new { success = true, message = thankYou });
                }

                var result = await _contactService.SubmitAsync(
                    model,
                    Request.UserHostAddress,
                    Request.UrlReferrer?.AbsolutePath);

                if (result == ContactSubmitResult.TooManyRequests)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Too many messages sent. Please try again in a few minutes."
                    });
                }

                return Json(new { success = true, message = thankYou });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "Something went wrong. Please try again later."
                });
            }
        }

        [ChildActionOnly]
        public ActionResult ContactInfo()
        {
            string ip = Request.UserHostAddress ?? "Unknown";
            // Use the same call/service your footer action uses to build LayoutDataVM
            LayoutDataVM layoutData = _webInfoService.GetLayoutData(ip);
            return PartialView("~/Views/Home/partial/_ContactInfo.cshtml", layoutData);
        }

        #region Bkup
        public ActionResult Index_Bk()
        {
            return View();
        }
        public ActionResult advantages_Bk()
        {
            return PartialView("~/Views/Home/partial/_advantages_Bk.cshtml");
        }
        public ActionResult milestones_Bk()
        {
            return PartialView("~/Views/Home/partial/_milestones_Bk.cshtml");
        }
        public ActionResult aitalent_Bk()
        {
            return PartialView("~/Views/Home/partial/_aitalent_Bk.cshtml");
        }

        [Route("about_bk")]
        public ActionResult About_Bk()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        [Route("contact-us_bk")]
        public async Task<ActionResult> Contact_Bk()
        {
            return View();
        }
        #endregion
    }
}
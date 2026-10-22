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

        // Read from <appSettings> key "Index" in Web.config
        private static readonly string HomePageId =
            ConfigurationManager.AppSettings["Index"];

        public HomeController(LogErrorService logErrorService,  UIPageService uiPageService)
        {
            _logErrorService = logErrorService;
            _uiPageService = uiPageService;
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


        public ActionResult advantages(
            List<DTOs.UISectionVM> sections)
        {
            return PartialView(
                "~/Views/Home/partial/_advantages.cshtml",
                sections
            );
        }
        public ActionResult milestones()
        {
            return PartialView(
                "~/Views/Home/partial/_milestones.cshtml");
        }

        public ActionResult aitalent()
        {
            return PartialView(
                "~/Views/Home/partial/_aitalent.cshtml");
        }

        public ActionResult Index_Bk()
        {
            return View();
        }

        public ActionResult advantages_Bk()
        {
            return PartialView(
                "~/Views/Home/partial/_advantages_Bk.cshtml");
        }

        public ActionResult milestones_Bk()
        {
            return PartialView(
                "~/Views/Home/partial/_milestones_Bk.cshtml");
        }

        public ActionResult aitalent_Bk()
        {
            return PartialView(
                "~/Views/Home/partial/_aitalent_Bk.cshtml");
        }

        [Route("about")]
        public ActionResult About()
        {
            ViewBag.Message =
                "Your application description page.";

            return View();
        }

        [Route("contact-us")]
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
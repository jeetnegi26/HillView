
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using DTOs;
using Services;

namespace HillView.sln.Controllers
{
    public class HomeController : Controller
    {
        private readonly SectionService _sectionService;
        private readonly ImageService _imageService;
        private readonly ButtonService _buttonService;
        private readonly HomePageService _homePageService;
        private readonly LogErrorService _logErrorService;

        private static readonly Guid HomePageId =
            new Guid("35BBA058-A5CA-F111-A44C-E867CD6913A5");

        public HomeController(
            SectionService sectionService,
            ImageService imageService,
            ButtonService buttonService,
            LogErrorService logErrorService,
            HomePageService homePageService)
        {
            _sectionService = sectionService;
            _imageService = imageService;
            _buttonService = buttonService;
            _logErrorService = logErrorService;
            _homePageService = homePageService;
        }
        public async Task<ActionResult> Index()
        {
            try
            {
                string ip = Request.UserHostAddress ?? "Unknown";


                var homePage = await _homePageService
                    .GetHomePageDataAsync(HomePageId, ip);

                return View(homePage);
            }
            catch (Exception ex)
            {
                return View("Error",
                    new HandleErrorInfo(ex, "Home", "Index"));
            }
        }

        public ActionResult advantages()
        {
            return PartialView(
                "~/Views/Home/partial/_advantages.cshtml");
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

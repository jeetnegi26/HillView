using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HillView.sln.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult advantages()
        {
            return PartialView("~/Views/Home/partial/_advantages.cshtml");
        }
        public ActionResult milestones()
        {
            return PartialView("~/Views/Home/partial/_milestones.cshtml");
        }
        public ActionResult aitalent()
        {
            return PartialView("~/Views/Home/partial/_aitalent.cshtml");
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
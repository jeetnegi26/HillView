using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HillView.sln.Controllers
{
    public class JobsController : Controller
    {
        // GET: Jobs
        public ActionResult Index()
        {
            return View();
        }
    }
}
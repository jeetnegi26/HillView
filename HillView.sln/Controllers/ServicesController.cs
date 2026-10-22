using Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace HillView.sln.Controllers
{
    public class ServicesController : Controller
    {
        private readonly LogErrorService _logErrorService;
        private readonly UIPageService _uiPageService;

        // Read from <appSettings> key "Index" in Web.config
        private static readonly string ServicePageId =
            ConfigurationManager.AppSettings["Service_Index"];


        public ServicesController(LogErrorService logErrorService, UIPageService uiPageService)
        {
            _logErrorService = logErrorService;
            _uiPageService = uiPageService;
        }
        // GET: Services
        public async Task<ActionResult> Index()
        {
            try
            {
                if (!Guid.TryParse(ServicePageId, out var pageId))
                {
                    throw new InvalidOperationException("AppSettings key 'Service_Index' is missing or is not a valid GUID.");
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

        public ActionResult Index_bk()
        {
            return View();
        }
    }
}
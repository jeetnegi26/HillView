using Services;
using Services.Admin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace HillView.sln.Controllers
{
    public class CaseStudyController : Controller
    {
        private readonly LogErrorService _logErrorService;
        private readonly UIPageService _uiPageService;
        private readonly BlogService _blogService;

        // Read from <appSettings> key "Index" in Web.config
        private static readonly string CaseStudyId = ConfigurationManager.AppSettings["CaseStudy_Index"];
        public CaseStudyController(LogErrorService logErrorService, 
            UIPageService uiPageService, BlogService blogService)
        {
            _logErrorService = logErrorService;
            _uiPageService = uiPageService;
            _blogService = blogService;
        }
        // GET: CaseStudy Listing Page
        public async Task<ActionResult> Index()
        {
            try
            {
                if (!Guid.TryParse(CaseStudyId, out var pageId))
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

        [HttpGet]
        public async Task<JsonResult> LoadCaseStudies(int page = 1)
        {
            try
            {
                var studies = await _blogService.GetCaseStudiesPagedAsync(page, 20, Request.UserHostAddress);
                return Json(new
                {
                    success = true,
                    items = studies,
                    hasMore = studies.Count == 20
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex); // Use your existing logging method.
                return Json(new
                {
                    success = false,
                    items = new List<DTOs.Blogs>(),
                    hasMore = false
                }, JsonRequestBehavior.AllowGet);
            }
        }
        [Route("CaseStudy/Study/{slug}")]
        public async Task<ActionResult> Study(string slug)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slug))
                    return HttpNotFound();

                // Get the selected case study using its slug.
                var currentStudy = await _blogService.GetCaseStudyBySlugAsync(
                    slug,
                    Request.UserHostAddress
                );

                if (currentStudy == null)
                    return HttpNotFound();

                // Get the latest 3 case studies, excluding the current one.
                var recentStudies = await _blogService.GetRecentCaseStudiesAsync(
                    currentStudy.Id,
                    3,
                    Request.UserHostAddress
                );

                var model = new DTOs.CaseStudyDetailVM
                {
                    CurrentStudy = currentStudy,
                    RecentStudies = recentStudies
                };

                return View(model);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}


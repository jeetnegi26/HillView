using DTOs;
using Services;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace HillView.sln.Controllers
{
    public class LayoutController : Controller
    {
        private readonly WebInfoService _webInfoService;

        public LayoutController(WebInfoService webInfoService)
        {
            _webInfoService = webInfoService;
        }


        [ChildActionOnly]
        public ActionResult SharedData()
        {
            string ip = Request.UserHostAddress ?? "Unknown";

            LayoutDataVM layoutData = _webInfoService.GetLayoutData(ip);

            return PartialView("_Header", layoutData);
        }
        [ChildActionOnly]
        public ActionResult Footer()
        {
            string ip = Request.UserHostAddress ?? "Unknown";
            LayoutDataVM layoutData = _webInfoService.GetLayoutData(ip);

            return PartialView("_Footer", layoutData);
        }
    }
}

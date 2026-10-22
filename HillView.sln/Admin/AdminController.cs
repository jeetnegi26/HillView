using DTOs;
using HillView.sln.App_Start;
using HillView.sln.Controllers;
using Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Services;

namespace HillView.sln.Admin
{
    [CustomAuthorize]
    public class AdminController : Controller
    {
        private readonly WebInfoService _webService;
        private readonly ImageService _imageService;
        private readonly LogErrorService _logErrorService;
        private readonly WebInfoService _webInfoService;
        private readonly PageService _pageService;
        private readonly SectionTypeService _sectionTypeService;
        private readonly SectionService _sectionService;
        private readonly SectionItemService _sectionItemService;
        private readonly ButtonService _buttonService;
        private readonly PageSectionService _pageSectionService;
        private readonly AdminMenuService _adminMenuService;
        private readonly TestimonialService _testimonialService;
        private readonly ContactUsService _contactUsService;

        public AdminController(WebInfoService webService, LogErrorService logErrorService, ImageService imageService, WebInfoService webInfoService, 
            PageService pageService, SectionTypeService sectionTypeService, SectionService sectionService, SectionItemService sectionItemService,
            ButtonService buttonService, PageSectionService pageSectionService, AdminMenuService adminMenuService, TestimonialService testimonialService,
            ContactUsService contactUsService
          )
        {
            _webService = webService;
            _logErrorService = logErrorService;
            _imageService = imageService;
            _webInfoService = webInfoService;
            _pageService = pageService;
            _sectionTypeService = sectionTypeService;
            _sectionService = sectionService;
            _sectionItemService = sectionItemService;
            _buttonService = buttonService;
            _adminMenuService = adminMenuService;
            _pageSectionService = pageSectionService;
            _testimonialService = testimonialService;
            _contactUsService = contactUsService;
        }

        #region Home
        // GET: Admin
        [Route("admin/dashboard")]
        public ActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<JsonResult> GetSidebarLogo()
        {
            var logo = await _webInfoService.GetBrandLogoAsync();

            return Json(new
            {
                success = logo != null,
                logo = logo
            }, JsonRequestBehavior.AllowGet);
        }
        [ChildActionOnly]
        public ActionResult AdminSidebar()
        {
            try
            {
                var ip = Request.UserHostAddress;

                var sidebar = _adminMenuService
                    .GetSidebarMenu(ip);

                return PartialView(
                    "~/Views/Shared/Admin/_AdminSidebar.cshtml",
                    sidebar);
            }
            catch (Exception ex)
            {
                var ip = Request.UserHostAddress;

                _logErrorService.LogError(
                    ex, "Admin/Sidebar", "GET", 0, ip, "AdminSidebar");

                return PartialView(
                    "~/Views/Shared/Admin/_AdminSidebar.cshtml",
                    new AdminSidebarVM());
            }
        }
        public async Task<ActionResult> MenuManagement()
        {
            try
            {
                var ip = Request.UserHostAddress;

                var menu = await _adminMenuService.GetMenuTreeAsync(ip);

                var sidebar = new DTOs.AdminSidebarVM();

                // Organize the returned menu items into the sidebar model.
                // The service already builds the hierarchy, so preserve
                // its existing item structure.
                foreach (var item in menu)
                {
                    if (item.GroupId == null)
                    {
                        sidebar.StandaloneItems.Add(item);
                    }
                    else
                    {
                        var group = sidebar.Groups.FirstOrDefault(
                            g => g.Id == item.GroupId.Value);

                        if (group == null)
                        {
                            group = new DTOs.AdminMenuGroupVM
                            {
                                Id = item.GroupId.Value,
                                Title = "Menu Group",
                                Items = new List<DTOs.AdminMenuItemVM>()
                            };

                            sidebar.Groups.Add(group);
                        }

                        group.Items.Add(item);
                    }
                }

                return View(sidebar);
            }
            catch (Exception ex)
            {
                var ip = Request.UserHostAddress;

                // Keep the same error-logging call used by your project.
                _logErrorService.LogError(ex, ip);

                return View("Error",
                    new HandleErrorInfo(ex, "Admin", "MenuManagement"));
            }
        }
        [HttpGet]
        public async Task<ActionResult> AddMenuItem()
        {
            try
            {
                var ip = Request.UserHostAddress;

                var model = await _adminMenuService.GetMenuItemFormDataAsync(ip);

                return View(model);
            }
            catch (Exception ex)
            {
                var ip = Request.UserHostAddress;
                _logErrorService.LogError(ex, ip);

                return View("Error",
                    new HandleErrorInfo(ex, "Admin", "AddMenuItem"));
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveMenuItem(DTOs.AdminMenuItemEditVM model)
        {
            var ip = Request.UserHostAddress;

            if (!ModelState.IsValid)
            {
                model = await _adminMenuService.GetMenuItemFormDataAsync(ip);
                return View("AddMenuItem", model);
            }

            try
            {
                if (model.Id.HasValue)
                {
                    await _adminMenuService.UpdateMenuItemAsync(model, ip);
                    TempData["SuccessMessage"] = "Menu item updated successfully.";
                }
                else
                {
                    await _adminMenuService.SaveMenuItemAsync(model, ip);
                    TempData["SuccessMessage"] = "Menu item added successfully.";
                }


                return RedirectToAction("MenuManagement");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);

                var formData = await _adminMenuService.GetMenuItemFormDataAsync(ip);

                formData.Title = model.Title;
                formData.Icon = model.Icon;
                formData.Controller = model.Controller;
                formData.Action = model.Action;
                formData.Url = model.Url;
                formData.GroupId = model.GroupId;
                formData.ParentMenuItemId = model.ParentMenuItemId;
                formData.DisplayOrder = model.DisplayOrder;
                formData.Status = model.Status;

                return View("AddMenuItem", formData);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, ip);

                ModelState.AddModelError("", "Unable to save the menu item.");

                var formData = await _adminMenuService.GetMenuItemFormDataAsync(ip);

                formData.Title = model.Title;
                formData.Icon = model.Icon;
                formData.Controller = model.Controller;
                formData.Action = model.Action;
                formData.Url = model.Url;
                formData.GroupId = model.GroupId;
                formData.ParentMenuItemId = model.ParentMenuItemId;
                formData.DisplayOrder = model.DisplayOrder;
                formData.Status = model.Status;

                return View("AddMenuItem", formData);
            }
        }
        [HttpGet]
        public async Task<ActionResult> EditMenuItem(Guid id)
        {
            try
            {
                var ip = Request.UserHostAddress;

                var model = await _adminMenuService.GetMenuItemByIdAsync(id, ip);

                if (model == null)
                {
                    return HttpNotFound();
                }

                return View("AddMenuItem", model);
            }
            catch (Exception ex)
            {
                var ip = Request.UserHostAddress;
                _logErrorService.LogError(ex, ip);

                return View("Error",
                    new HandleErrorInfo(ex, "Admin", "EditMenuItem"));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteMenuItem(Guid id)
        {
            var ip = Request.UserHostAddress;

            try
            {
                await _adminMenuService.DeleteMenuItemAsync(id, ip);

                TempData["SuccessMessage"] = "Menu item deleted successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, ipAddress: ip);
                TempData["ErrorMessage"] = "Unable to delete the menu item.";
            }

            return RedirectToAction("MenuManagement");
        }
        #endregion

        #region Pages CRUD
        [HttpGet]
        public async Task<ActionResult> Pages()
        {
            try
            {
                var pages = await _pageService.GetPagesAsync(
                    Request.UserHostAddress);

                return View(pages);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Pages", "GET", 0,
                    Request.UserHostAddress, "GetPages");

                return View("Error");
            }
        }
        [HttpGet]
        public async Task<ActionResult> PageAction(Guid? id)
        {
            try
            {
                PageVM model = new PageVM();

                if (id.HasValue && id.Value != Guid.Empty)
                {
                    model = await _pageService.GetPageByIdAsync(
                        id.Value, Request.UserHostAddress);

                    if (model == null)
                        return HttpNotFound();
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/PageAction", "GET", 0,
                    Request.UserHostAddress, "GetPageById");

                return View("Error");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public async Task<ActionResult> PageAction(PageVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                bool saved = await _pageService.SavePageAsync(
                    model, Request.UserHostAddress);

                if (!saved)
                {
                    ModelState.AddModelError(
                        "",
                        "The page could not be saved. Please try again.");

                    return View(model);
                }

                TempData["PageMessage"] = "Page saved successfully.";
                _webInfoService.ClearLayoutDataCache();
                return RedirectToAction("Pages");
            }
            catch (System.Data.SqlClient.SqlException ex)
                when (ex.Number == 2601 || ex.Number == 2627)
            {
                ModelState.AddModelError(
                    nameof(model.Slug),
                    "This URL slug already exists. Please use another slug.");

                return View(model);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/PageAction", "POST", 0,
                    Request.UserHostAddress, "SavePage");

                ModelState.AddModelError(
                    "",
                    "Unable to save the page. Please try again.");

                return View(model);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeletePage(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid page ID."
                    });
                }

                bool deleted = await _pageService.DeletePageAsync(
                    id, Request.UserHostAddress);
                _webInfoService.ClearLayoutDataCache();
                return Json(new
                {
                    success = deleted,
                    message = deleted
                        ? "Page deleted successfully."
                        : "Page not found."
                });
            }
            catch (System.Data.SqlClient.SqlException ex)
                when (ex.Number == 547)
            {
                return Json(new
                {
                    success = false,
                    message = "This page has sections assigned to it. Remove those assignments before deleting it."
                });
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/DeletePage", "POST", 0,
                    Request.UserHostAddress, "DeletePage");

                return Json(new
                {
                    success = false,
                    message = "Unable to delete the page. Please try again."
                });
            }
        }
        [HttpGet]
        public async Task<ActionResult> PageContent(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Invalid page ID.";
                    return RedirectToAction("Pages");
                }

                var page = await _pageService.GetPageByIdAsync(
                    id, Request.UserHostAddress);

                if (page == null)
                {
                    TempData["ErrorMessage"] = "Page not found.";
                    return RedirectToAction("Pages");
                }

                var pageSections = await _pageSectionService
                    .GetPageSectionsAsync(Request.UserHostAddress);

                var assignedSections = pageSections
                    .Where(x => x.PageId == id)
                    .OrderBy(x => x.DisplayOrder)
                    .ToList();

                ViewBag.PageTitle = page.Title;
                ViewBag.PageId = page.Id;

                return View(assignedSections);
            }
            catch (Exception ex)
            {
                 _logErrorService.LogError(
                    ex,
                    Request.Url?.ToString(),
                    nameof(PageContent),
                    0,
                    Request.UserHostAddress,
                    null);

                TempData["ErrorMessage"] =
                    "Unable to load the page content editor.";

                return RedirectToAction("Pages");
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetSectionContent(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid section ID."
                    }, JsonRequestBehavior.AllowGet);
                }

                var section = await _sectionService.GetSectionByIdAsync(
                    id, Request.UserHostAddress);

                if (section == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Section not found."
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = section.Id,
                        title = section.Title,
                        heading = section.Heading,
                        subHeading = section.SubHeading,
                        description = section.Description,
                        backgroundColor = section.BackgroundColor,
                        cssClass = section.CssClass
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                 _logErrorService.LogError(
                    ex,
                    Request.Url?.ToString(),
                    nameof(GetSectionContent),
                    0,
                    Request.UserHostAddress,
                    null);

                return Json(new
                {
                    success = false,
                    message = "Unable to load section content."
                }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveSectionContent(SectionVM model)
        {
            try
            {
                if (model == null || model.Id == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid section."
                    });
                }

                // Only allow fields supported by the inline editor.
                var existing = await _sectionService.GetSectionByIdAsync(
                    model.Id, Request.UserHostAddress);

                if (existing == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Section not found."
                    });
                }

                existing.Heading = model.Heading;
                existing.SubHeading = model.SubHeading;
                existing.Description = model.Description;
                existing.BackgroundColor = model.BackgroundColor;
                existing.CssClass = model.CssClass;

                var saved = await _sectionService.SaveSectionAsync(
                    existing, Request.UserHostAddress);

                return Json(new
                {
                    success = saved,
                    message = saved
                        ? "Section content saved successfully."
                        : "Unable to save section content."
                });
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    Request.Url?.ToString(),
                    nameof(SaveSectionContent),
                    0,
                    Request.UserHostAddress,
                    null);

                return Json(new
                {
                    success = false,
                    message = "An error occurred while saving section content."
                });
            }
        }
        #endregion

        #region Section Types CRUD
        [HttpGet]
        public async Task<ActionResult> SectionTypes()
        {
            try
            {
                var sectionTypes = await _sectionTypeService
                    .GetSectionTypesAsync(Request.UserHostAddress);

                return View(sectionTypes);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionTypes", "GET", 0,
                    Request.UserHostAddress, "GetSectionTypes");

                return View("Error");
            }
        }
        [HttpGet]
        public async Task<ActionResult> SectionTypeAction(
            int? id, bool validation = false)
        {
            try
            {
                SectionTypeVM model = new SectionTypeVM();

                if (validation)
                {
                    model = TempData["SectionTypeFormModel"]
                        as SectionTypeVM;

                    var errors = TempData["SectionTypeModelErrors"]
                        as Dictionary<string, string[]>;

                    if (model == null)
                        return RedirectToAction("SectionTypes");

                    if (errors != null)
                    {
                        foreach (var entry in errors)
                        {
                            foreach (var error in entry.Value)
                            {
                                ModelState.AddModelError(
                                    entry.Key, error);
                            }
                        }
                    }

                    return View(model);
                }

                if (id.HasValue && id.Value > 0)
                {
                    model = await _sectionTypeService.GetSectionTypeByIdAsync(
                        id.Value, Request.UserHostAddress);

                    if (model == null)
                        return HttpNotFound();
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionTypeAction", "GET", id ?? 0,
                    Request.UserHostAddress, "GetSectionTypeById");

                return View("Error");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SectionTypeAction(SectionTypeVM model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool saved = await _sectionTypeService
                        .SaveSectionTypeAsync(
                            model, Request.UserHostAddress);

                    if (saved)
                    {
                        TempData["SectionTypeSuccess"] =
                            "Section type saved successfully.";

                        return RedirectToAction("SectionTypes");
                    }

                    ModelState.AddModelError(
                        "",
                        "The section type could not be saved. Please try again.");
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
                when (ex.Number == 2601 || ex.Number == 2627)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "This section type name already exists.");
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionTypeAction", "POST", model.Id,
                    Request.UserHostAddress, "SaveSectionType");

                ModelState.AddModelError(
                    "",
                    "Unable to save the section type. Please try again.");
            }

            // Preserve the form values and validation errors for the GET request.
            TempData["SectionTypeFormModel"] = model;

            TempData["SectionTypeModelErrors"] = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? "The submitted value is invalid."
                            : e.ErrorMessage)
                        .ToArray());

            return RedirectToAction(
                "SectionTypeAction",
                new
                {
                    id = model.Id > 0 ? (int?)model.Id : null,
                    validation = true
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteSection(Guid id, Guid? pageId)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Invalid section ID.";
                    return RedirectToAction("Sections");
                }

                bool deleted = await _sectionService.DeleteSectionAsync(
                    id, Request.UserHostAddress);

                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] =
                    deleted
                        ? "Section deleted successfully."
                        : "Section not found.";
            }
            catch (System.Data.SqlClient.SqlException ex)
                when (ex.Number == 547)
            {
                TempData["ErrorMessage"] =
                    "This section is assigned to a page or has related content. Remove those assignments before deleting it.";
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Sections", "DELETE", 0,
                    Request.UserHostAddress, "DeleteSection");

                TempData["ErrorMessage"] =
                    "Unable to delete the section. Please try again.";
            }

            if (pageId.HasValue && pageId.Value != Guid.Empty)
            {
                return RedirectToAction("PageContent", new { id = pageId.Value });
            }

            return RedirectToAction("Sections");
        }
        #endregion

        #region Sections CRUD
        [HttpGet]
        public async Task<ActionResult> Sections()
        {
            try
            {
                var sections = await _sectionService.GetSectionsAsync(
                    Request.UserHostAddress);

                return View(sections);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Sections", "GET", 0,
                    Request.UserHostAddress, "Sections");

                return View("Error");
            }
        }
        [HttpGet]
        public async Task<ActionResult> SectionAction(Guid? id, bool validation = false)
        {
            SectionVM model;

            if (validation && TempData["SectionFormModel"] is SectionVM savedModel)
            {
                model = savedModel;

                if (TempData["SectionFormErrors"] is Dictionary<string, string[]> errors)
                {
                    foreach (var item in errors)
                    {
                        foreach (var error in item.Value)
                        {
                            ModelState.AddModelError(item.Key, error);
                        }
                    }
                }
            }
            else
            {
                model = new SectionVM();

                if (id.HasValue && id.Value != Guid.Empty)
                {
                    try
                    {
                        model = await _sectionService.GetSectionByIdAsync(
                            id.Value, Request.UserHostAddress);

                        if (model == null)
                            return HttpNotFound();
                    }
                    catch (Exception ex)
                    {
                        _logErrorService.LogError(
                            ex, "Admin/SectionAction", "GET", 0,
                            Request.UserHostAddress, "SectionAction");

                        return View("Error");
                    }
                }
            }

            await LoadSectionTypesAsync(model.SectionTypeId);
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public async Task<ActionResult> SectionAction(SectionVM model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool saved = await _sectionService.SaveSectionAsync(
                        model, Request.UserHostAddress);

                    if (saved)
                    {
                        TempData["SuccessMessage"] = "Section saved successfully.";
                        return RedirectToAction("Sections");
                    }

                    ModelState.AddModelError("", "No section was saved. Please try again.");
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
                when (ex.Number == 547)
            {
                ModelState.AddModelError(
                    "SectionTypeId",
                    "The selected section type is invalid or is being used by another record.");
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionAction", "POST", 0,
                    Request.UserHostAddress, "SectionAction");

                ModelState.AddModelError(
                    "", "Unable to save the section. Please try again.");
            }

            // Preserve the form and validation errors using Post/Redirect/Get.
            TempData["SectionFormModel"] = model;

            TempData["SectionFormErrors"] = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? "The submitted value is invalid."
                            : e.ErrorMessage)
                        .ToArray());

            return RedirectToAction(
                "SectionAction",
                new { id = model.Id == Guid.Empty ? (Guid?)null : model.Id, validation = true });
        }
        private async Task LoadSectionTypesAsync(int selectedId = 0)
        {
            var sectionTypes = await _sectionService.GetSectionTypesAsync(
                Request.UserHostAddress);

            ViewBag.SectionTypes = new SelectList(
                sectionTypes, "Id", "Title", selectedId);
        }
        #endregion

        #region Section Items CRUD
        //[HttpGet]
        //public async Task<ActionResult> SectionItems()
        //{
        //    try
        //    {
        //        var items = await _sectionItemService.GetSectionItemsAsync(
        //            Request.UserHostAddress);

        //        return View(items);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logErrorService.LogError(
        //            ex, "Admin/SectionItems", "GET",
        //            null, Request.UserHostAddress, "SectionItems");

        //        return View("Error");
        //    }
        //}
        [HttpGet]
        public async Task<ActionResult> SectionItems(Guid? sectionId)
        {
            try
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                if (sectionId.HasValue && sectionId.Value != Guid.Empty)
                {
                    var section = await _sectionService.GetSectionByIdAsync(
                        sectionId.Value, ip);

                    if (section == null)
                    {
                        TempData["ErrorMessage"] = "Section not found.";
                        return RedirectToAction("Sections");
                    }

                    var items = await _sectionItemService
                        .GetSectionItemsBySectionIdAsync(sectionId.Value, ip);

                    ViewBag.SectionId = sectionId.Value;
                    ViewBag.SectionTitle = section.Title;

                    return View(items);
                }

                var allItems = await _sectionItemService
                    .GetSectionItemsAsync(ip);

                ViewBag.SectionId = null;
                ViewBag.SectionTitle = null;

                return View(allItems);
            }
            catch (Exception ex)
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                _logErrorService.LogError(
                    ex,
                    "Admin/SectionItems",
                    "GET",
                    null,
                    ip,
                    "SectionItems");

                return View("Error");
            }
        }
        [HttpGet]
        public async Task<ActionResult> SectionItemAction(Guid? id, Guid? sectionId, bool validation = false)
        {
            SectionItemVM model;

            if (validation &&
                TempData["SectionItemFormModel"] is SectionItemVM savedModel)
            {
                model = savedModel;

                if (TempData["SectionItemFormErrors"]
                    is Dictionary<string, string[]> errors)
                {
                    foreach (var item in errors)
                    {
                        foreach (var error in item.Value)
                        {
                            ModelState.AddModelError(item.Key, error);
                        }
                    }
                }
            }
            else
            {
                model = new SectionItemVM
                {
                    SectionId = sectionId ?? Guid.Empty
                };

                if (id.HasValue && id.Value != Guid.Empty)
                {
                    try
                    {
                        model = await _sectionItemService.GetSectionItemByIdAsync(
                            id.Value, Request.UserHostAddress);

                        if (model == null)
                            return HttpNotFound();
                    }
                    catch (Exception ex)
                    {
                        _logErrorService.LogError(
                            ex, "Admin/SectionItemAction", "GET",
                            null, Request.UserHostAddress,
                            "GetSectionItemByIdAsync");

                        return View("Error");
                    }
                }
            }

            await LoadSectionsForItemAsync(model.SectionId);

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public async Task<ActionResult> SectionItemAction(SectionItemVM model)
        {
            if (model.SectionId == Guid.Empty)
            {
                ModelState.AddModelError(
                    "SectionId", "Please select a valid section.");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    Guid? savedId = await _sectionItemService
                        .SaveSectionItemAsync(model, Request.UserHostAddress);

                    if (savedId.HasValue)
                    {
                        TempData["SuccessMessage"] =
                            "Section item saved successfully.";

                        return RedirectToAction(
                            "SectionItemAction",
                            new { id = savedId.Value }
                        );
                    }

                    ModelState.AddModelError(
                        "", "No section item was saved. Please try again.");
                }
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                ModelState.AddModelError(
                    "SectionId",
                    "The selected section is invalid or cannot be used.");
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionItemAction", "POST",
                    null, Request.UserHostAddress, "SaveSectionItemAsync");

                ModelState.AddModelError(
                    "", "Unable to save the section item. Please try again.");
            }

            TempData["SectionItemFormModel"] = model;

            TempData["SectionItemFormErrors"] = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value.Errors.Select(e =>
                        string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? "The submitted value is invalid."
                            : e.ErrorMessage).ToArray());

            return RedirectToAction(
                "SectionItemAction",
                new
                {
                    id = model.Id == Guid.Empty ? (Guid?)null : model.Id,
                    validation = true
                });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteSectionItem(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Invalid section item ID.";
                    return RedirectToAction("SectionItems");
                }

                bool deleted = await _sectionItemService.DeleteSectionItemAsync(
                    id, Request.UserHostAddress);

                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] =
                    deleted
                        ? "Section item deleted successfully."
                        : "Section item not found.";
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["ErrorMessage"] =
                    "This item has related records. Remove those references before deleting it.";
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/SectionItems", "DELETE",
                    null, Request.UserHostAddress, "DeleteSectionItem");

                TempData["ErrorMessage"] =
                    "Unable to delete the section item. Please try again.";
            }

            return RedirectToAction("SectionItems");
        }
        private async Task LoadSectionsForItemAsync(Guid selectedSectionId)
        {
            var sections = await _sectionItemService.GetSectionsAsync(
                Request.UserHostAddress);

            ViewBag.Sections = new SelectList(
                sections, "Id", "Title", selectedSectionId);
        }
        [HttpGet]
        public async Task<JsonResult> GetSectionItemsContent(Guid sectionId)
        {
            try
            {
                if (sectionId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid section ID."
                    }, JsonRequestBehavior.AllowGet);
                }

                // Confirm the section exists.
                var section = await _sectionService.GetSectionByIdAsync(
                    sectionId, Request.UserHostAddress);

                if (section == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Section not found."
                    }, JsonRequestBehavior.AllowGet);
                }

                var items = await _sectionItemService
                    .GetSectionItemsBySectionIdAsync(
                        sectionId, Request.UserHostAddress);

                return Json(new
                {
                    success = true,
                    data = items.Select(item => new
                    {
                        id = item.Id,
                        sectionId = item.SectionId,
                        title = item.Title,
                        heading = item.Heading,
                        subHeading = item.SubHeading,
                        description = item.Description,
                        value = item.Value,
                        label = item.Label,
                        linkUrl = item.LinkUrl,
                        additionalData = item.AdditionalData,
                        displayOrder = item.DisplayOrder,
                        status = item.Status
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                 _logErrorService.LogError(
                    ex,
                    Request.Url?.ToString(),
                    nameof(GetSectionItemsContent),
                    0,
                    Request.UserHostAddress,
                    null);

                return Json(new
                {
                    success = false,
                    message = "Unable to load section items."
                }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteSectionItemContent(Guid id)
        {
            try
            {
                var ip = Request.UserHostAddress ?? "Unknown";

                if (id == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid card ID."
                    });
                }

                bool deleted = await _sectionItemService
                    .DeleteSectionItemAsync(id, ip);

                if (!deleted)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Card not found or already deleted."
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Card deleted successfully."
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while deleting the card."
                });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveSectionItemContent(SectionItemVM model)
        {
            try
            {
                var ip = Request.UserHostAddress ?? "Unknown";

                if (model.SectionId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid section."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Card title is required."
                    });
                }

                if (model.DisplayOrder < 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Display order cannot be negative."
                    });
                }

                Guid? savedId = await _sectionItemService
                    .SaveSectionItemAsync(model, ip);

                if (!savedId.HasValue)
                {
                    return Json(new
                    {
                        success = false,
                        message = "The card could not be saved."
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Card saved successfully.",
                    id = savedId.Value
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while saving the card."
                });
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetSectionItemImages(Guid sectionItemId)
        {
            try
            {
                var ip = Request.UserHostAddress ?? "Unknown";

                if (sectionItemId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid card ID."
                    }, JsonRequestBehavior.AllowGet);
                }

                var images = await _imageService
                    .GetImagesBySectionItemIdAsync(sectionItemId, ip);

                return Json(new
                {
                    success = true,
                    images = images.Select(image => new
                    {
                        id = image.Id,
                        sectionItemId = image.SectionItemId,
                        imageUrl = image.ImageUrl,
                        altText = image.AltText,
                        caption = image.Caption,
                        imageType = image.ImageType,
                        displayOrder = image.DisplayOrder,
                        status = image.Status
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while loading card images."
                }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdateSectionItemImage(Guid id, string altText, string caption, int displayOrder, bool status)
        {
            try
            {
                var ip = Request.UserHostAddress ?? "Unknown";

                if (id == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid image ID."
                    });
                }

                var image = await _imageService.GetImageByIdAsync(id, ip);

                if (image == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Image not found."
                    });
                }

                if (displayOrder < 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Display order cannot be negative."
                    });
                }

                image.AltText = altText;
                image.Caption = caption;
                image.DisplayOrder = displayOrder;
                image.Status = status;

                bool saved = await _imageService.SaveImageAsync(image, ip);

                return Json(new
                {
                    success = saved,
                    message = saved
                        ? "Image details updated successfully."
                        : "Unable to update image details."
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while updating the image."
                });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteSectionItemImage(Guid id)
        {
            try
            {
                var ip = Request.UserHostAddress ?? "Unknown";

                if (id == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid image ID."
                    });
                }

                var image = await _imageService.GetImageByIdAsync(id, ip);

                if (image == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Image not found."
                    });
                }

                // Resolve the physical file path.
                string imageUrl = image.ImageUrl ?? string.Empty;
                string fileName = Path.GetFileName(imageUrl);

                if (string.IsNullOrWhiteSpace(fileName) ||
                    fileName != imageUrl)
                {
                    // Support older records containing a relative upload path.
                    fileName = Path.GetFileName(imageUrl.Replace('\\', '/'));
                }

                string folderPath = HostingEnvironment.MapPath("~/Content/Uploads/");

                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    throw new InvalidOperationException(
                        "Unable to resolve the image upload directory.");
                }

                string physicalPath = Path.Combine(folderPath, fileName);

                // Delete the database record first.
                bool deleted = await _imageService.DeleteImageAsync(id, ip);

                if (!deleted)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Unable to delete the image record."
                    });
                }


                bool fileCleanupFailed = false;

                try
                {
                    bool isReferenced = await _imageService.IsImageUrlReferencedAsync(
                        image.ImageUrl,
                        id,
                        ip);

                    if (!isReferenced && System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }
                catch (Exception fileEx)
                {
                    fileCleanupFailed = true;

                    _logErrorService.LogError(
                        fileEx,
                        "Admin/DeleteSectionItemImage/FileCleanup",
                        "POST",
                        0,
                        ip,
                        "Card image record deleted, but physical-file cleanup failed.");
                }

                return Json(new
                {
                    success = true,
                    fileCleanupFailed = fileCleanupFailed,
                    message = fileCleanupFailed
                        ? "Image record deleted, but its physical file could not be removed. Please check the server logs."
                        : "Image deleted successfully."
                });

            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/DeleteSectionItemImage",
                    "POST",
                    0,
                    Request.UserHostAddress,
                    "Delete Section Item Image");

                return Json(new
                {
                    success = false,
                    message = "An error occurred while deleting the image."
                });
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetSectionImages(Guid sectionId)
        {
            try
            {
                var ip = Request.UserHostAddress ?? "Unknown";

                if (sectionId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid section ID."
                    }, JsonRequestBehavior.AllowGet);
                }

                var images = await _imageService
                    .GetImagesBySectionIdAsync(sectionId, ip);

                return Json(new
                {
                    success = true,
                    images = images.Select(image => new
                    {
                        id = image.Id,
                        sectionId = image.SectionId,
                        imageUrl = image.ImageUrl,
                        altText = image.AltText,
                        caption = image.Caption,
                        imageType = image.ImageType,
                        displayOrder = image.DisplayOrder,
                        status = image.Status
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/GetSectionImages",
                    "GET",
                    0,
                    Request.UserHostAddress,
                    "Get Section Images");

                return Json(new
                {
                    success = false,
                    message = "An error occurred while loading section images."
                }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdateSectionImage(
            Guid id,
            Guid sectionId,
            string altText,
            string caption,
            int displayOrder,
            bool status)
        {
            try
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                if (id == Guid.Empty || sectionId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid image or section ID."
                    });
                }

                if (displayOrder < 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Display order cannot be negative."
                    });
                }

                var image = await _imageService.GetImageByIdAsync(id, ip);

                // Ensure this image belongs to the specified section,
                // not to a section item/card.
                if (image == null ||
                    image.SectionId != sectionId ||
                    image.SectionItemId.HasValue)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Section image not found."
                    });
                }

                image.AltText = altText;
                image.Caption = caption;
                image.DisplayOrder = displayOrder;
                image.Status = status;

                bool saved = await _imageService.SaveImageAsync(image, ip);

                return Json(new
                {
                    success = saved,
                    message = saved
                        ? "Section image updated successfully."
                        : "Unable to update section image."
                });
            }
            catch (Exception ex)
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                _logErrorService.LogError(
                    ex,
                    "Admin/UpdateSectionImage",
                    "POST",
                    0,
                    ip,
                    "Update Section Image");

                Response.StatusCode = 500;

                return Json(new
                {
                    success = false,
                    message = "An error occurred while updating the section image."
                });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> DeleteSectionImage(Guid id, Guid sectionId)
        {
            try
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                if (id == Guid.Empty || sectionId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid image or section ID."
                    });
                }

                var image = await _imageService.GetImageByIdAsync(id, ip);

                // Ensure this is an image belonging directly to the section.
                if (image == null ||
                    image.SectionId != sectionId ||
                    image.SectionItemId.HasValue)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Section image not found."
                    });
                }

                // Support filename-only and legacy relative-path records.
                string storedUrl = (image.ImageUrl ?? "").Replace('\\', '/');
                string fileName = System.IO.Path.GetFileName(storedUrl);

                bool isLegacyUploadPath =
                    storedUrl.StartsWith(
                        "~/Content/Uploads/",
                        StringComparison.OrdinalIgnoreCase) ||
                    storedUrl.StartsWith(
                        "Content/Uploads/",
                        StringComparison.OrdinalIgnoreCase);

                bool isFilenameOnly =
                    !string.IsNullOrWhiteSpace(storedUrl) &&
                    storedUrl == fileName;

                if (string.IsNullOrWhiteSpace(fileName) ||
                    (!isFilenameOnly && !isLegacyUploadPath))
                {
                    return Json(new
                    {
                        success = false,
                        message = "The stored image path is invalid."
                    });
                }

                string uploadFolder = Server.MapPath("~/Content/Uploads/");
                string fullUploadFolder =
                    System.IO.Path.GetFullPath(uploadFolder);

                string physicalPath = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(fullUploadFolder, fileName));

                // Prevent deleting files outside the uploads directory.
                string allowedPrefix =
                    fullUploadFolder.TrimEnd(
                        System.IO.Path.DirectorySeparatorChar,
                        System.IO.Path.AltDirectorySeparatorChar)
                    + System.IO.Path.DirectorySeparatorChar;

                if (!physicalPath.StartsWith(
                        allowedPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new
                    {
                        success = false,
                        message = "The image path is invalid."
                    });
                }

                bool deleted = await _imageService.DeleteImageAsync(id, ip);

                if (!deleted)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Unable to delete the section image record."
                    });
                }


                bool fileCleanupFailed = false;

                try
                {
                    bool isReferenced = await _imageService.IsImageUrlReferencedAsync(
                        image.ImageUrl,
                        id,
                        ip);

                    if (!isReferenced && System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }
                catch (Exception fileEx)
                {
                    fileCleanupFailed = true;

                    _logErrorService.LogError(
                        fileEx,
                        "Admin/DeleteSectionImage/FileCleanup",
                        "POST",
                        0,
                        ip,
                        "Section image record deleted, but physical-file cleanup failed.");
                }

                return Json(new
                {
                    success = true,
                    fileCleanupFailed = fileCleanupFailed,
                    message = fileCleanupFailed
                        ? "Image record deleted, but its physical file could not be removed. Please check the server logs."
                        : "Section image deleted successfully."
                });

            }
            catch (Exception ex)
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                _logErrorService.LogError(
                    ex,
                    "Admin/DeleteSectionImage",
                    "POST",
                    0,
                    ip,
                    "Delete Section Image");

                Response.StatusCode = 500;

                return Json(new
                {
                    success = false,
                    message = "An error occurred while deleting the section image."
                });
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetSectionButtons(Guid sectionId)
        {
            try
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                if (sectionId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid section ID."
                    }, JsonRequestBehavior.AllowGet);
                }

                var buttons = await _buttonService.GetButtonsAsync(ip);

                var sectionButtons = buttons
                    .Where(b => b.SectionId == sectionId &&
                                !b.SectionItemId.HasValue)
                    .OrderBy(b => b.DisplayOrder)
                    .Select(b => new
                    {
                        id = b.Id,
                        sectionId = b.SectionId,
                        title = b.Title,
                        url = b.Url,
                        target = b.Target,
                        cssClass = b.CssClass,
                        buttonType = b.ButtonType,
                        displayOrder = b.DisplayOrder,
                        status = b.Status
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    buttons = sectionButtons
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                _logErrorService.LogError(
                    ex,
                    "Admin/GetSectionButtons",
                    "GET",
                    0,
                    ip,
                    "Get Section Buttons");

                Response.StatusCode = 500;

                return Json(new
                {
                    success = false,
                    message = "Unable to load section buttons."
                }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveSectionButton(
            Guid sectionId,
            Guid? id,
            string title,
            string url,
            string target,
            string cssClass,
            string buttonType,
            int displayOrder,
            bool status)
        {
            try
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                if (sectionId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid section ID."
                    });
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Button title is required."
                    });
                }

                if (displayOrder < 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Display order cannot be negative."
                    });
                }

                ButtonVM button;

                if (id.HasValue && id.Value != Guid.Empty)
                {
                    button = await _buttonService.GetButtonByIdAsync(id.Value, ip);

                    if (button == null ||
                        button.SectionId != sectionId ||
                        button.SectionItemId.HasValue)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Section button not found."
                        });
                    }
                }
                else
                {
                    button = new ButtonVM
                    {
                        Id = Guid.Empty,
                        SectionId = sectionId,
                        SectionItemId = null,
                        Status = true
                    };
                }

                button.SectionId = sectionId;
                button.SectionItemId = null;
                button.Title = title.Trim();
                button.Url = url?.Trim();
                button.Target = string.IsNullOrWhiteSpace(target)
                    ? "_self"
                    : target.Trim();
                button.CssClass = cssClass?.Trim();
                button.ButtonType = buttonType?.Trim();
                button.DisplayOrder = displayOrder;
                button.Status = status;

                bool saved = await _buttonService.SaveButtonAsync(button, ip);

                return Json(new
                {
                    success = saved,
                    message = saved
                        ? "Section button saved successfully."
                        : "Unable to save section button."
                });
            }
            catch (Exception ex)
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                _logErrorService.LogError(
                    ex,
                    "Admin/SaveSectionButton",
                    "POST",
                    0,
                    ip,
                    "Save Section Button");

                Response.StatusCode = 500;

                return Json(new
                {
                    success = false,
                    message = "An error occurred while saving the section button."
                });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteSectionButton(Guid sectionId, Guid id)
        {
            var ip = Request.UserHostAddress ?? string.Empty;

            try
            {
                var button = await _buttonService.GetButtonByIdAsync(id, ip);

                if (button == null ||
                    button.SectionId != sectionId ||
                    button.SectionItemId.HasValue)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Button not found for this section."
                    });
                }

                bool deleted = await _buttonService.DeleteButtonAsync(id, ip);

                return Json(new
                {
                    success = deleted,
                    message = deleted
                        ? "Button deleted successfully."
                        : "Button could not be deleted."
                });
            }
            catch (Exception ex)
            {
                 _logErrorService.LogError(ex, ip);

                return Json(new
                {
                    success = false,
                    message = "An error occurred while deleting the button."
                });
            }
        }
        [HttpGet]
        public async Task<ActionResult> GetSectionItemButtons(Guid sectionItemId)
        {
            var ip = Request.UserHostAddress ?? string.Empty;

            try
            {
                var buttons = await _buttonService.GetButtonsAsync(ip);

                var result = buttons
                    .Where(b => b.SectionItemId == sectionItemId
                             && !b.SectionId.HasValue)
                    .OrderBy(b => b.DisplayOrder)
                    .Select(b => new
                    {
                        id = b.Id,
                        sectionItemId = b.SectionItemId,
                        title = b.Title,
                        url = b.Url,
                        target = b.Target,
                        cssClass = b.CssClass,
                        buttonType = b.ButtonType,
                        displayOrder = b.DisplayOrder,
                        status = b.Status
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    buttons = result
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                 _logErrorService.LogError(ex, ip);

                return Json(new
                {
                    success = false,
                    message = "Unable to load card buttons."
                }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveSectionItemButton(
            Guid sectionItemId,
            Guid? id,
            string title,
            string url,
            string target,
            string cssClass,
            string buttonType,
            int displayOrder,
            bool status)
        {
            var ip = Request.UserHostAddress ?? string.Empty;
            try
            {
                if (sectionItemId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Please select a valid card."
                    });
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Button text is required."
                    });
                }

                if (displayOrder < 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Display order cannot be negative."
                    });
                }

                ButtonVM model;

                if (id.HasValue && id.Value != Guid.Empty)
                {
                    model = await _buttonService.GetButtonByIdAsync(id.Value, ip);

                    if (model == null ||
                        model.SectionItemId != sectionItemId ||
                        model.SectionId.HasValue)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Button not found for this card."
                        });
                    }
                }
                else
                {
                    model = new ButtonVM
                    {
                        Id = Guid.Empty,
                        SectionId = null,
                        SectionItemId = sectionItemId
                    };
                }

                model.Title = title.Trim();
                model.Url = url;
                model.Target = target;
                model.CssClass = cssClass;
                model.ButtonType = buttonType;
                model.DisplayOrder = displayOrder;
                model.Status = status;

                await _buttonService.SaveButtonAsync(model, ip);

                return Json(new
                {
                    success = true,
                    message = "Card button saved successfully."
                });
            }
            catch (Exception ex)
            {
                 _logErrorService.LogError(ex, ip);

                return Json(new
                {
                    success = false,
                    message = "An error occurred while saving the card button."
                });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteSectionItemButton(Guid sectionItemId, Guid id)
        {
            var ip = Request.UserHostAddress ?? string.Empty;

            try
            {
                var button = await _buttonService.GetButtonByIdAsync(id, ip);

                if (button == null ||
                    button.SectionItemId != sectionItemId ||
                    button.SectionId.HasValue)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Button not found for this card."
                    });
                }

                bool deleted = await _buttonService.DeleteButtonAsync(id, ip);

                return Json(new
                {
                    success = deleted,
                    message = deleted
                        ? "Card button deleted successfully."
                        : "Card button could not be deleted."
                });
            }
            catch (Exception ex)
            {
                 _logErrorService.LogError(ex, ip);

                return Json(new
                {
                    success = false,
                    message = "An error occurred while deleting the card button."
                });
            }
        }
        #endregion

        #region Section Item Content
        [HttpGet]
        public async Task<JsonResult> GetChildSectionItems(Guid parentSectionItemId)
        {
            try
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                if (parentSectionItemId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid parent item ID."
                    }, JsonRequestBehavior.AllowGet);
                }

                var parent = await _sectionItemService.GetSectionItemByIdAsync(
                    parentSectionItemId, ip);

                if (parent == null || parent.ParentSectionItemId.HasValue)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Parent section item not found."
                    }, JsonRequestBehavior.AllowGet);
                }

                var children = await _sectionItemService.GetChildSectionItemsAsync(
                    parentSectionItemId, ip);

                return Json(new
                {
                    success = true,
                    data = children.Select(item => new
                    {
                        id = item.Id,
                        sectionId = item.SectionId,
                        parentSectionItemId = item.ParentSectionItemId,
                        title = item.Title,
                        heading = item.Heading,
                        subHeading = item.SubHeading,
                        description = item.Description,
                        value = item.Value,
                        label = item.Label,
                        linkUrl = item.LinkUrl,
                        additionalData = item.AdditionalData,
                        displayOrder = item.DisplayOrder,
                        status = item.Status
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                _logErrorService.LogError(
                    ex,
                    Request.Url?.ToString(),
                    nameof(GetChildSectionItems),
                    0,
                    ip,
                    null);

                return Json(new
                {
                    success = false,
                    message = "Unable to load child items."
                }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveChildSectionItem(SectionItemVM model)
        {
            try
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                if (model == null ||
                    model.ParentSectionItemId == null ||
                    model.ParentSectionItemId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "A valid parent item is required."
                    });
                }

                if (model.SectionId == Guid.Empty)
                {
                    return Json(new
                    {
                        success = false,
                        message = "A valid section is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Child item title is required."
                    });
                }

                if (model.DisplayOrder < 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Display order cannot be negative."
                    });
                }

                var parent = await _sectionItemService.GetSectionItemByIdAsync(
                    model.ParentSectionItemId.Value, ip);

                if (parent == null ||
                    parent.ParentSectionItemId.HasValue ||
                    parent.SectionId != model.SectionId)
                {
                    return Json(new
                    {
                        success = false,
                        message = "The parent item is invalid for the selected section."
                    });
                }

                Guid? savedId = await _sectionItemService.SaveSectionItemAsync(
                    model, ip);

                if (!savedId.HasValue)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Unable to save the child item."
                    });
                }

                return Json(new
                {
                    success = true,
                    id = savedId.Value,
                    message = "Child item saved successfully."
                });
            }
            catch (Exception ex)
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                _logErrorService.LogError(
                    ex,
                    Request.Url?.ToString(),
                    nameof(SaveChildSectionItem),
                    0,
                    ip,
                    null);

                return Json(new
                {
                    success = false,
                    message = "An error occurred while saving the child item."
                });
            }
        }
        #endregion

        #region Images CRUD
        [HttpGet]
        public async Task<ActionResult> Images()
        {
            try
            {
                var images = await _imageService.GetImagesAsync(
                    Request.UserHostAddress);

                return View(images);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Images", "GET", 0,
                    Request.UserHostAddress, "Images");

                return View("Error");
            }
        }
        [HttpGet]
        public async Task<ActionResult> ImageAction(Guid? id, Guid? sectionId, Guid? sectionItemId, bool validation = false)
        {
            ImageVM model;

            if (validation &&
                TempData["ImageFormModel"] is ImageVM savedModel)
            {
                model = savedModel;

                if (TempData["ImageFormErrors"]
                    is Dictionary<string, string[]> errors)
                {
                    foreach (var item in errors)
                    {
                        foreach (var error in item.Value)
                        {
                            ModelState.AddModelError(item.Key, error);
                        }
                    }
                }
            }
            else
            {
                model = new ImageVM
                {
                    SectionId = sectionId,
                    SectionItemId = sectionItemId
                };

                if (id.HasValue && id.Value != Guid.Empty)
                {
                    try
                    {
                        model = await _imageService.GetImageByIdAsync(
                            id.Value, Request.UserHostAddress);

                        if (model == null)
                            return HttpNotFound();
                    }
                    catch (Exception ex)
                    {
                        _logErrorService.LogError(
                            ex, "Admin/ImageAction", "GET", 0,
                            Request.UserHostAddress, "GetImageByIdAsync");

                        return View("Error");
                    }
                }
            }

            await LoadImageDropdownsAsync(model);

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ImageAction(ImageVM model)
        {
            bool hasSection = model.SectionId.HasValue &&
                              model.SectionId.Value != Guid.Empty;

            bool hasSectionItem = model.SectionItemId.HasValue &&
                                  model.SectionItemId.Value != Guid.Empty;

            if (hasSection == hasSectionItem)
            {
                ModelState.AddModelError(
                    "",
                    "Select either a section or a section item, but not both.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    bool saved = await _imageService.SaveImageAsync(
                        model, Request.UserHostAddress);

                    if (saved)
                    {
                        TempData["SuccessMessage"] = "Image saved successfully.";
                        return RedirectToAction("Images");
                    }

                    ModelState.AddModelError(
                        "", "No image record was saved. Please try again.");
                }
                catch (SqlException ex) when (ex.Number == 547)
                {
                    ModelState.AddModelError(
                        "", "The selected section or section item is invalid.");
                }
                catch (Exception ex)
                {
                    _logErrorService.LogError(
                        ex, "Admin/ImageAction", "POST", 0,
                        Request.UserHostAddress, "SaveImageAsync");

                    ModelState.AddModelError(
                        "", "Unable to save the image. Please try again.");
                }
            }

            TempData["ImageFormModel"] = model;

            TempData["ImageFormErrors"] = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value.Errors.Select(e =>
                        string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? "The submitted value is invalid."
                            : e.ErrorMessage).ToArray());

            return RedirectToAction(
                "ImageAction",
                new
                {
                    id = model.Id == Guid.Empty ? (Guid?)null : model.Id,
                    validation = true
                });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteImage(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Invalid image ID.";
                    return RedirectToAction("Images");
                }

                // Retrieve the URL before deleting the database record.
                var image = await _imageService.GetImageByIdAsync(
                    id, Request.UserHostAddress);

                if (image == null)
                {
                    TempData["ErrorMessage"] = "Image not found.";
                    return RedirectToAction("Images");
                }

                bool deleted = await _imageService.DeleteImageAsync(
                    id, Request.UserHostAddress);

                if (deleted)
                {
                    TempData["SuccessMessage"] = "Image record deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Image record could not be deleted.";
                }
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Images", "DELETE", 0,
                    Request.UserHostAddress, "DeleteImage");

                TempData["ErrorMessage"] =
                    "Unable to delete the image. Please try again.";
            }

            return RedirectToAction("Images");
        }
        private async Task LoadImageDropdownsAsync(ImageVM model)
        {
            string ip = Request.UserHostAddress;

            var sections = await _imageService.GetSectionsAsync(ip);

            ViewBag.Sections = new SelectList(
                sections, "Id", "Title", model.SectionId);

            var sectionItems = new List<SectionItemVM>();

            if (model.SectionId.HasValue &&
                model.SectionId.Value != Guid.Empty)
            {
                sectionItems = await _sectionItemService
                    .GetSectionItemsBySectionIdAsync(
                        model.SectionId.Value, ip);
            }

            ViewBag.SectionItems = new SelectList(
                sectionItems, "Id", "Title", model.SectionItemId);
        }
        [HttpGet]
        public async Task<JsonResult> GetImageSectionItems(Guid sectionId)
        {
            if (sectionId == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    items = new object[0]
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var items = await _sectionItemService
                    .GetSectionItemsBySectionIdAsync(
                        sectionId, Request.UserHostAddress);

                return Json(new
                {
                    success = true,
                    items = items.Select(item => new
                    {
                        id = item.Id,
                        title = item.Title
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/GetImageSectionItems",
                    "GET",
                    0,
                    Request.UserHostAddress,
                    "GetImageSectionItems");

                Response.StatusCode = 500;

                return Json(new
                {
                    success = false,
                    items = new object[0]
                }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region Upload Content Image
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Upload_Content_Image(HttpPostedFileBase file, Guid? sectionItemId, Guid? sectionId, string altText,string caption)
        {
            try
            {

                bool hasSectionItem = sectionItemId.HasValue &&
                                      sectionItemId.Value != Guid.Empty;

                bool hasSection = sectionId.HasValue &&
                                  sectionId.Value != Guid.Empty;

                if (hasSectionItem == hasSection)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Select exactly one section or card for this image."
                    });
                }


                if (file == null || file.ContentLength == 0)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Please select an image file."
                    });
                }

                const int maxFileSize = 15 * 1024 * 1024;

                if (file.ContentLength > maxFileSize)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Maximum image size is 15 MB."
                    });
                }

                string extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };

                if (string.IsNullOrWhiteSpace(extension) ||
                    !allowedExtensions.Contains(extension))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Only JPG, PNG, WebP, SVG and GIF files are allowed."
                    });
                }

                // Validate the file signature instead of trusting the extension alone.
                // Validate the file signature instead of trusting the extension alone.
                byte[] signature = new byte[12];

                int bytesRead = await file.InputStream.ReadAsync(
                    signature, 0, signature.Length);

                file.InputStream.Position = 0;

                bool validSignature =
                    (extension == ".jpg" || extension == ".jpeg")
                        ? bytesRead >= 3 &&
                          signature[0] == 0xFF &&
                          signature[1] == 0xD8 &&
                          signature[2] == 0xFF
                    : extension == ".png"
                        ? bytesRead >= 8 &&
                          signature[0] == 0x89 &&
                          signature[1] == 0x50 &&
                          signature[2] == 0x4E &&
                          signature[3] == 0x47 &&
                          signature[4] == 0x0D &&
                          signature[5] == 0x0A &&
                          signature[6] == 0x1A &&
                          signature[7] == 0x0A
                    : extension == ".gif"
                        ? bytesRead >= 6 &&
                          System.Text.Encoding.ASCII.GetString(signature, 0, 6)
                              .StartsWith("GIF8")
                    : extension == ".webp"
                        ? bytesRead >= 12 &&
                          System.Text.Encoding.ASCII.GetString(signature, 0, 4) == "RIFF" &&
                          System.Text.Encoding.ASCII.GetString(signature, 8, 4) == "WEBP"
                    : extension == ".svg"
                        ? IsValidSvg(file.InputStream)
                    : false;

                if (!validSignature)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "The file contents do not match a supported image format."
                    });
                }

                string folderPath = HostingEnvironment.MapPath("/Content/Uploads/");

                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    throw new InvalidOperationException(
                        "Unable to resolve the image upload directory.");
                }

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Generate a unique filename to prevent overwriting existing images.
                string fileName = Guid.NewGuid().ToString("N") + extension;
                string physicalPath = Path.Combine(folderPath, fileName);

                await Task.Run(() => file.SaveAs(physicalPath));

                var image = new ImageVM
                {
                    Id = Guid.Empty,

                    // Associate the image with the selected parent or child item.
                    SectionId = hasSection ? sectionId : null,
                    SectionItemId = hasSectionItem ? sectionItemId : null,

                    // Save the filename, not the physical disk path.
                    ImageUrl = fileName,

                    AltText = altText,
                    Caption = caption,
                    ImageType = "Content",
                    DisplayOrder = 0,
                    Status = true
                };

                //bool saved = await _imageService.SaveImageAsync(image, Request.UserHostAddress);

                //if (!saved)
                //{
                //    // Remove the physical file if its database record was not saved.
                //    if (System.IO.File.Exists(physicalPath))
                //    {
                //        System.IO.File.Delete(physicalPath);
                //    }

                //    return Json(new
                //    {
                //        Success = false,
                //        Message = "The image file was uploaded, but its database record could not be saved."
                //    });
                //}

                return Json(new
                {
                    Success = true,
                    ImageUrl = fileName,
                    Message = "Image uploaded successfully."
                });

            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/Upload_Content_Image",
                    "POST",
                    0,
                    Request.UserHostAddress,
                    "Content Image Upload");

                Response.StatusCode = 500;

                return Json(new
                {
                    Success = false,
                    Message = "Unable to upload the image. Please try again."
                });
            }
        }
        private bool IsValidSvg(Stream stream)
        {
            try
            {
                stream.Position = 0;

                var settings = new System.Xml.XmlReaderSettings
                {
                    DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                    XmlResolver = null
                };

                using (var reader = System.Xml.XmlReader.Create(stream, settings))
                {
                    var document = System.Xml.Linq.XDocument.Load(reader);

                    return document.Root != null &&
                           document.Root.Name.LocalName == "svg" &&
                           document.Root.Name.NamespaceName ==
                               "http://www.w3.org/2000/svg";
                }
            }
            catch (System.Xml.XmlException)
            {
                return false;
            }
            finally
            {
                if (stream.CanSeek)
                    stream.Position = 0;
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Upload_PageContent_Image(HttpPostedFileBase file, Guid? sectionItemId, Guid? sectionId, string altText, string caption)
        {
            try
            {

                bool hasSectionItem = sectionItemId.HasValue &&
                                      sectionItemId.Value != Guid.Empty;

                bool hasSection = sectionId.HasValue &&
                                  sectionId.Value != Guid.Empty;

                if (hasSectionItem == hasSection)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Select exactly one section or card for this image."
                    });
                }


                if (file == null || file.ContentLength == 0)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Please select an image file."
                    });
                }

                const int maxFileSize = 15 * 1024 * 1024;

                if (file.ContentLength > maxFileSize)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Maximum image size is 15 MB."
                    });
                }

                string extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

                string[] allowedExtensions =
  {
    ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg"
};

                if (string.IsNullOrWhiteSpace(extension) ||
                    !allowedExtensions.Contains(extension))
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "Only JPG, PNG, WebP, SVG and GIF files are allowed."
                    });
                }

                // Validate the file signature instead of trusting the extension alone.
                // Validate the file signature instead of trusting the extension alone.
                byte[] signature = new byte[12];

                int bytesRead = await file.InputStream.ReadAsync(
                    signature, 0, signature.Length);

                file.InputStream.Position = 0;

                bool validSignature =
                    (extension == ".jpg" || extension == ".jpeg")
                        ? bytesRead >= 3 &&
                          signature[0] == 0xFF &&
                          signature[1] == 0xD8 &&
                          signature[2] == 0xFF
                    : extension == ".png"
                        ? bytesRead >= 8 &&
                          signature[0] == 0x89 &&
                          signature[1] == 0x50 &&
                          signature[2] == 0x4E &&
                          signature[3] == 0x47 &&
                          signature[4] == 0x0D &&
                          signature[5] == 0x0A &&
                          signature[6] == 0x1A &&
                          signature[7] == 0x0A
                    : extension == ".gif"
                        ? bytesRead >= 6 &&
                          System.Text.Encoding.ASCII.GetString(signature, 0, 6)
                              .StartsWith("GIF8")
                    : extension == ".webp"
                        ? bytesRead >= 12 &&
                          System.Text.Encoding.ASCII.GetString(signature, 0, 4) == "RIFF" &&
                          System.Text.Encoding.ASCII.GetString(signature, 8, 4) == "WEBP"
                    : extension == ".svg"
                        ? IsValidSvg(file.InputStream)
                    : false;

                if (!validSignature)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "The file contents do not match a supported image format."
                    });
                }

                string folderPath = HostingEnvironment.MapPath("/Content/Uploads/");

                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    throw new InvalidOperationException(
                        "Unable to resolve the image upload directory.");
                }

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Generate a unique filename to prevent overwriting existing images.
                string fileName = Guid.NewGuid().ToString("N") + extension;
                string physicalPath = Path.Combine(folderPath, fileName);

                await Task.Run(() => file.SaveAs(physicalPath));


                string imageUrl = fileName;


                var image = new ImageVM
                {
                    Id = Guid.Empty,

                    SectionId = hasSection
                        ? sectionId.Value
                        : (Guid?)null,

                    SectionItemId = hasSectionItem
                        ? sectionItemId.Value
                        : (Guid?)null,

                    ImageUrl = imageUrl,
                    AltText = altText,
                    Caption = caption,
                    ImageType = hasSection ? "Section" : "Card",
                    DisplayOrder = 1,
                    Status = true
                };


                string ip = Request.UserHostAddress ?? "Unknown";

                bool saved = await _imageService.SaveImageAsync(image, ip);

                if (!saved)
                {
                    // Remove the uploaded file if the database save fails.
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }

                    return Json(new
                    {
                        Success = false,
                        Message = "The image was uploaded but could not be saved to the database."
                    });
                }

                return Json(new
                {
                    Success = true,
                    ImageUrl = imageUrl,
                    Message = "Image uploaded and saved successfully."
                });

            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/Upload_Content_Image",
                    "POST",
                    0,
                    Request.UserHostAddress,
                    "Content Image Upload");

                Response.StatusCode = 500;

                return Json(new
                {
                    Success = false,
                    Message = "Unable to upload the image. Please try again."
                });
            }
        }
        #endregion

        #region Buttons CRUD

        [HttpGet]
        public async Task<ActionResult> Buttons()
        {
            try
            {
                var buttons = await _buttonService.GetButtonsAsync(
                    Request.UserHostAddress);

                return View(buttons);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Buttons", "GET", 0,
                    Request.UserHostAddress, "Buttons");

                return View("Error");
            }
        }

        [HttpGet]
        public async Task<ActionResult> ButtonAction(Guid? id, Guid? sectionId, Guid? sectionItemId, bool validation = false)
        {
            ButtonVM model;

            if (validation &&
                TempData["ButtonFormModel"] is ButtonVM savedModel)
            {
                model = savedModel;

                if (TempData["ButtonFormErrors"]
                    is Dictionary<string, string[]> errors)
                {
                    foreach (var item in errors)
                    {
                        foreach (var error in item.Value)
                        {
                            ModelState.AddModelError(item.Key, error);
                        }
                    }
                }
            }
            else
            {
                model = new ButtonVM
                {
                    SectionId = sectionId,
                    SectionItemId = sectionItemId
                };

                if (id.HasValue && id.Value != Guid.Empty)
                {
                    try
                    {
                        model = await _buttonService.GetButtonByIdAsync(
                            id.Value, Request.UserHostAddress);

                        if (model == null)
                            return HttpNotFound();
                    }
                    catch (Exception ex)
                    {
                        _logErrorService.LogError(
                            ex, "Admin/ButtonAction", "GET", 0,
                            Request.UserHostAddress, "GetButtonByIdAsync");

                        return View("Error");
                    }
                }
            }

            await LoadButtonDropdownsAsync(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ButtonAction(ButtonVM model)
        {
            bool hasSection = model.SectionId.HasValue &&
                              model.SectionId.Value != Guid.Empty;

            bool hasSectionItem = model.SectionItemId.HasValue &&
                                  model.SectionItemId.Value != Guid.Empty;

            if (hasSection == hasSectionItem)
            {
                ModelState.AddModelError(
                    "",
                    "Select either a section or a section item, but not both.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    bool saved = await _buttonService.SaveButtonAsync(
                        model, Request.UserHostAddress);

                    if (saved)
                    {
                        TempData["SuccessMessage"] =
                            "Button saved successfully.";

                        return RedirectToAction("Buttons");
                    }

                    ModelState.AddModelError(
                        "", "No button was saved. Please try again.");
                }
                catch (SqlException ex) when (ex.Number == 547)
                {
                    ModelState.AddModelError(
                        "",
                        "The selected section or section item is invalid.");
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logErrorService.LogError(
                        ex, "Admin/ButtonAction", "POST", 0,
                        Request.UserHostAddress, "SaveButtonAsync");

                    ModelState.AddModelError(
                        "", "Unable to save the button. Please try again.");
                }
            }

            TempData["ButtonFormModel"] = model;

            TempData["ButtonFormErrors"] = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value.Errors.Select(e =>
                        string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? "The submitted value is invalid."
                            : e.ErrorMessage).ToArray());

            return RedirectToAction(
                "ButtonAction",
                new
                {
                    id = model.Id == Guid.Empty ? (Guid?)null : model.Id,
                    validation = true
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteButton(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Invalid button ID.";
                    return RedirectToAction("Buttons");
                }

                bool deleted = await _buttonService.DeleteButtonAsync(
                    id, Request.UserHostAddress);

                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] =
                    deleted
                        ? "Button deleted successfully."
                        : "Button not found.";
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["ErrorMessage"] =
                    "The button cannot be deleted because it is referenced by another record.";
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, "Admin/Buttons", "DELETE", 0,
                    Request.UserHostAddress, "DeleteButton");

                TempData["ErrorMessage"] =
                    "Unable to delete the button. Please try again.";
            }

            return RedirectToAction("Buttons");
        }

        private async Task LoadButtonDropdownsAsync(ButtonVM model)
        {
            string ip = Request.UserHostAddress;

            var sections = await _buttonService.GetSectionsAsync(ip);
            var sectionItems = await _buttonService.GetSectionItemsAsync(ip);

            ViewBag.Sections = new SelectList(
                sections, "Id", "Title", model.SectionId);

            ViewBag.SectionItems = new SelectList(
                sectionItems, "Id", "Title", model.SectionItemId);
        }

        #endregion

        #region PageSections CRUD
        [HttpGet]
        public async Task<ActionResult> PageSections()
        {
            try
            {
                var pageSections = await _pageSectionService
                    .GetPageSectionsAsync(Request.UserHostAddress);

                return View(pageSections);
            }
            catch (Exception ex)
            {
                 _logErrorService.LogError(
                    ex, Request.Url?.ToString(), nameof(PageSections),
                    0, Request.UserHostAddress, null);

                TempData["ErrorMessage"] =
                    "Unable to load page section assignments.";

                return View(new List<PageSectionVM>());
            }
        }
        [HttpGet]
        public async Task<ActionResult> PageSectionAction(Guid? id, bool validation = false)
        {
            PageSectionVM model;

            if (validation)
            {
                model = TempData["PageSectionFormModel"]
                    as PageSectionVM ?? new PageSectionVM();

                var errors = TempData["PageSectionFormErrors"]
                    as Dictionary<string, string[]>;

                if (errors != null)
                {
                    foreach (var item in errors)
                    {
                        foreach (var error in item.Value)
                        {
                            ModelState.AddModelError(item.Key, error);
                        }
                    }
                }
            }
            else if (id.HasValue && id.Value != Guid.Empty)
            {
                model = await _pageSectionService
                    .GetPageSectionByIdAsync(
                        id.Value, Request.UserHostAddress);

                if (model == null)
                {
                    TempData["ErrorMessage"] =
                        "Page section assignment not found.";

                    return RedirectToAction("PageSections");
                }
            }
            else
            {
                model = new PageSectionVM
                {
                    Status = true,
                    DisplayOrder = 0
                };
            }

            await LoadPageSectionDropdownsAsync(model);

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PageSectionAction(PageSectionVM model)
        {
            if (model.PageId == Guid.Empty)
            {
                ModelState.AddModelError(
                    nameof(model.PageId), "Please select a page.");
            }

            if (model.SectionId == Guid.Empty)
            {
                ModelState.AddModelError(
                    nameof(model.SectionId), "Please select a section.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    bool saved = await _pageSectionService
                        .SavePageSectionAsync(
                            model, Request.UserHostAddress);

                    if (saved)
                    {
                        TempData["SuccessMessage"] =
                            "Page section assignment saved successfully.";

                        return RedirectToAction("PageSections");
                    }

                    ModelState.AddModelError(
                        "", "Unable to save the page section assignment.");
                }
                catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                {
                    ModelState.AddModelError(
                        "",
                        "This section is already assigned to the selected page.");
                }
                catch (SqlException ex) when (ex.Number == 547)
                {
                    ModelState.AddModelError(
                        "",
                        "The selected page or section no longer exists.");
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                     _logErrorService.LogError(
                        ex, Request.Url?.ToString(),
                        nameof(PageSectionAction), 0,
                        Request.UserHostAddress, null);

                    ModelState.AddModelError(
                        "", "An unexpected error occurred while saving.");
                }
            }

            TempData["PageSectionFormModel"] = model;

            TempData["PageSectionFormErrors"] = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value.Errors
                        .Select(e => !string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? e.ErrorMessage
                            : "The supplied value is invalid.")
                        .ToArray());

            return RedirectToAction(
                "PageSectionAction",
                new
                {
                    id = model.Id == Guid.Empty
                        ? (Guid?)null
                        : model.Id,
                    validation = true
                });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeletePageSection(Guid id, Guid pageId)
        {
            try
            {
                if (id == Guid.Empty || pageId == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Invalid page or section assignment ID.";
                    return RedirectToAction("PageSections");
                }

                bool deleted = await _pageSectionService
                    .DeletePageSectionAsync(id, Request.UserHostAddress);

                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] =
                    deleted
                        ? "Page section assignment removed successfully."
                        : "Page section assignment was not found.";
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex, Request.Url?.ToString(),
                    nameof(DeletePageSection), 0,
                    Request.UserHostAddress, null);

                TempData["ErrorMessage"] =
                    "Unable to remove the page section assignment.";
            }

            return RedirectToAction("PageContent", new { id = pageId });
        }
        private async Task LoadPageSectionDropdownsAsync(PageSectionVM model)
        {
            string ip = Request.UserHostAddress;

            var pages = await _pageSectionService.GetPagesAsync(ip);
            var sections = await _pageSectionService.GetSectionsAsync(ip);

            ViewBag.Pages = new SelectList(
                pages, "Id", "Title", model.PageId);

            ViewBag.Sections = new SelectList(
                sections, "Id", "Title", model.SectionId);
        }
        #endregion

        #region Testimonials CRUD

        [HttpGet]
        public async Task<ActionResult> Testimonials()
        {
            try
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                var testimonials = await _testimonialService
                    .GetTestimonialsAsync(ip);

                return View(testimonials);
            }
            catch (Exception ex)
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                _logErrorService.LogError(
                    ex,
                    "Admin/Testimonials",
                    "GET",
                    0,
                    ip,
                    "GetTestimonials");

                return View("Error");
            }
        }
        [HttpGet]
        public async Task<ActionResult> TestimonialAction(Guid? id)
        {
            try
            {
                string ip = Request.UserHostAddress ?? "Unknown";
                TestimonialVM model;

                if (id.HasValue && id.Value != Guid.Empty)
                {
                    model = await _testimonialService
                        .GetTestimonialByIdAsync(id.Value, ip);

                    if (model == null)
                    {
                        TempData["ErrorMessage"] = "Testimonial not found.";
                        return RedirectToAction("Testimonials");
                    }
                }
                else
                {
                    model = new TestimonialVM
                    {
                        Rating = 5,
                        DisplayOrder = 0,
                        Status = true
                    };
                }

                return View(model);
            }
            catch (Exception ex)
            {
                string ip = Request.UserHostAddress ?? "Unknown";

                _logErrorService.LogError(
                    ex,
                    "Admin/TestimonialAction",
                    "GET",
                    0,
                    ip,
                    "TestimonialAction");

                return View("Error");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public async Task<ActionResult> TestimonialAction(TestimonialVM model)
        {
            string ip = Request.UserHostAddress ?? "Unknown";

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Set audit information on the server.
                model.UserHostAdd = ip;

                // Set CreatedBy only for a new testimonial.
                // Existing testimonials retain their original CreatedBy.
                if (model.Id == Guid.Empty)
                {
                    model.CreatedBy = User.Identity != null
                        && User.Identity.IsAuthenticated
                        ? User.Identity.Name
                        : null;
                }

                Guid? savedId = await _testimonialService
                    .SaveTestimonialAsync(model, ip);

                if (!savedId.HasValue)
                {
                    ModelState.AddModelError(
                        "",
                        "The testimonial could not be saved.");

                    return View(model);
                }

                TempData["SuccessMessage"] = model.Id == Guid.Empty
                    ? "Testimonial added successfully."
                    : "Testimonial updated successfully.";

                return RedirectToAction("Testimonials");
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/TestimonialAction",
                    "POST",
                    0,
                    ip,
                    "SaveTestimonial");

                ModelState.AddModelError(
                    "",
                    "An error occurred while saving the testimonial.");

                return View(model);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteTestimonial(Guid id)
        {
            string ip = Request.UserHostAddress ?? "Unknown";

            try
            {
                if (id == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Invalid testimonial ID.";
                    return RedirectToAction("Testimonials");
                }

                bool deleted = await _testimonialService
                    .DeleteTestimonialAsync(id, ip);

                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] =
                    deleted
                        ? "Testimonial deleted successfully."
                        : "Testimonial not found.";
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/Testimonials",
                    "DELETE",
                    0,
                    ip,
                    "DeleteTestimonial");

                TempData["ErrorMessage"] =
                    "Unable to delete the testimonial. Please try again.";
            }

            return RedirectToAction("Testimonials");
        }

        #endregion

        #region Website Info
        public async Task<ActionResult> WebInfos()
        {
            var ip = Request.UserHostAddress.ToString();
            var webInfos = await _webService.GetWebInfoAsync(ip);
            return View(webInfos);
        }
        public async Task<ActionResult> WebInfoAction(int id = 0)
        {
            var data = new Key();
            var ip = Request.UserHostAddress.ToString();
            TempData["KeyId"] = id;
            if (id  > 0)
            {
                data = await _webService.GetWebInfoByIdAsync(id, ip);
                if (data.Type == "BrandLogo")
                {
                    TempData["OldImage"] = data.Name;
                }
            }
            return PartialView(data);
        }
        [ValidateInput(false)]
        [HttpPost]
        public async Task<ActionResult> WebInfoAction(Key Key, HttpPostedFileBase File)
        {
            var OldImage = TempData["OldImage"] != null ? TempData["OldImage"].ToString() : "";
            var result = new ResponseVM();
            var UserHostAdd = Request.UserHostAddress.ToString();
            //int Id = Convert.ToInt32(TempData["KeyId"]);
            //if (Id > 0)
            //{
            //    Key.Id = Id;
            //}
            if (!ModelState.IsValid)
            {
                TempData["KeyId"] = Key.Id;
                result.Success = false;
                result.Message = "Please fill all required fields.";
                return Json(new { Result = result });
            }
            result = await _webService.UpInsertWebInfoAsync(Key, UserHostAdd);
            if (!result.Success)
            {
                TempData["KeyId"] = Key.Id;
                return Json(new { Result = result });
            }
            if (File != null && result.Success)
            {
                await Upload_Logo_File(File, OldImage);
            }
            return Json(new { Result = result });
        }
        #endregion

        #region Contact Us

        // status: null = all, true = active, false = inactive
        [HttpGet]
        public async Task<ActionResult> ContactUs(bool? status)
        {
            string ip = Request.UserHostAddress ?? "Unknown";

            try
            {
                var contacts = await _contactUsService.GetContactsAsync(status, ip);

                ViewBag.StatusFilter = status;

                return View(contacts);
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/ContactUs",
                    "GET",
                    0,
                    ip,
                    "ContactUs");

                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangeContactStatus(Guid id, bool newStatus, bool? filter)
        {
            string ip = Request.UserHostAddress ?? "Unknown";

            try
            {
                if (id == Guid.Empty)
                {
                    TempData["ErrorMessage"] = "Invalid contact ID.";
                    return RedirectToAction("ContactUs", new { status = filter });
                }

                bool updated = await _contactUsService.SetStatusAsync(id, newStatus, ip);

                if (updated)
                    TempData["SuccessMessage"] = newStatus
                        ? "Contact marked as active."
                        : "Contact marked as inactive.";
                else
                    TempData["ErrorMessage"] = "Contact not found.";
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(
                    ex,
                    "Admin/ContactUs",
                    "POST",
                    0,
                    ip,
                    "ChangeContactStatus");

                TempData["ErrorMessage"] = "Unable to change the status. Please try again.";
            }

            // Return to the same filtered list the admin was viewing.
            return RedirectToAction("ContactUs", new { status = filter });
        }

        #endregion

        #region Upload Logo Image
        [HttpPost]
        public async Task<JsonResult> Upload_Logo_File(HttpPostedFileBase file, string oldImage)
        {
            JsonResult result = new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
            try
            {
                var basePath = HostingEnvironment.MapPath("~/Content/Logo/");
                var clientFolderPath = Path.Combine(basePath);
                if (!Directory.Exists(clientFolderPath))
                {
                    Directory.CreateDirectory(clientFolderPath);
                }
                // Delete the old file if it exists
                if (!string.IsNullOrEmpty(oldImage))
                {
                    string virtualPath = oldImage.StartsWith("~/") ? oldImage : $"~/Content/Logo/{oldImage}";
                    var oldFilePath = HostingEnvironment.MapPath(virtualPath);
                    try
                    {
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                    catch (Exception exDel)
                    {
                        _logErrorService.LogError(exDel, "Unable to delete old file: " + oldFilePath, "DELETE", 0, Request.UserHostAddress, "Logo Upload");
                    }
                }
                if (file != null)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var path = Path.Combine(clientFolderPath, fileName);
                    await Task.Run(() => file.SaveAs(path));
                }
                result.Data = new { Success = true, Message = "Images have been saved on the server successfully." };
            }
            catch (Exception ex)
            {
                _logErrorService.LogError(ex, "Admin/Upload_Logo_File", "POST", 0, Request.UserHostAddress, "Logo Upload");
                result.Data = new { Success = false, Message = "Oops! The images could not be saved on the server. " + ex.Message };
            }
            return result;
        }
        #endregion
    }
}



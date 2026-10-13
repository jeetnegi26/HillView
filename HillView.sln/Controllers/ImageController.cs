



//using Services;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Hosting;
//using System.Web.Mvc;

//namespace HillView.sln.Controllers
//{
//    public class ImageController : Controller
//    {

//        private readonly LogErrorService _logErrorService;
//        private readonly ImageService _imageService;

//        public ImageController(LogErrorService logErrorService, ImageService imageService)
//        {
//            _logErrorService = logErrorService;
//            _imageService = imageService;
//        }
//        // GET: Image
//        [HttpPost]
//        public async Task<JsonResult> UploadImage(
//            HttpPostedFileBase file,
//            string typeId,
//            string hostAdd,
//            string id = "")
//        {
//            try
//            {
//                if (file == null || file.ContentLength == 0)
//                {
//                    return Json(new
//                    {
//                        Success = false,
//                        Message = "Please select an image."
//                    });
//                }

//                if (string.IsNullOrWhiteSpace(typeId))
//                {
//                    return Json(new
//                    {
//                        Success = false,
//                        Message = "Image type is required."
//                    });
//                }

//                // Maximum upload size: 5 MB
//                if (file.ContentLength > 5 * 1024 * 1024)
//                {
//                    return Json(new
//                    {
//                        Success = false,
//                        Message = "Image size must not exceed 5 MB."
//                    });
//                }

//                // Allow only supported image extensions
//                var extension = Path.GetExtension(file.FileName);

//                var allowedExtensions = new[]
//                {
//            ".jpg", ".jpeg", ".png", ".webp"
//        };

//                if (string.IsNullOrWhiteSpace(extension) ||
//                    !allowedExtensions.Contains(
//                        extension,
//                        StringComparer.OrdinalIgnoreCase))
//                {
//                    return Json(new
//                    {
//                        Success = false,
//                        Message = "Only JPG, PNG and WEBP images are allowed."
//                    });
//                }

//                if (!string.IsNullOrWhiteSpace(id) &&
//                    !Guid.TryParse(id, out _))
//                {
//                    return Json(new
//                    {
//                        Success = false,
//                        Message = "Invalid image ID."
//                    });
//                }

//                if (string.IsNullOrWhiteSpace(hostAdd))
//                {
//                    hostAdd = Request.UserHostAddress;
//                }

//                var folderPath =
//                    HostingEnvironment.MapPath("~/Content/Images/");

//                if (string.IsNullOrWhiteSpace(folderPath))
//                    throw new InvalidOperationException(
//                        "Image upload directory is unavailable.");

//                Directory.CreateDirectory(folderPath);

//                string oldImageUrl = null;

//                if (!string.IsNullOrWhiteSpace(id))
//                {
//                    oldImageUrl = await _imageService.GetImageUrlAsync(id);
//                }

//                // Never use the client's original filename.
//                var fileName = Guid.NewGuid().ToString("N")
//                             + extension.ToLowerInvariant();

//                var physicalPath = Path.Combine(folderPath, fileName);

//                // Store a relative path in SQL Server.
//                var relativePath = "/Content/Images/" + fileName;

//                try
//                {
//                    file.SaveAs(physicalPath);

//                    var savedId = await _imageService.UpInsertImageAsync(
//                        relativePath,
//                        typeId,
//                        hostAdd,
//                        id
//                    );


//                    // Database operation succeeded; now clean up the old file.
//                    if (!string.IsNullOrWhiteSpace(oldImageUrl) &&
//                        !string.Equals(
//                            oldImageUrl,
//                            relativePath,
//                            StringComparison.OrdinalIgnoreCase))
//                    {
//                        var oldFileName = Path.GetFileName(oldImageUrl);
//                        var imageFolder = HostingEnvironment.MapPath("~/Content/Images/");
//                        var oldPhysicalPath = Path.Combine(imageFolder, oldFileName);

//                        if (System.IO.File.Exists(oldPhysicalPath))
//                        {
//                            System.IO.File.Delete(oldPhysicalPath);
//                        }
//                    }


//                    return Json(new
//                    {
//                        Success = true,
//                        Message = "Image saved successfully.",
//                        Id = savedId
//                    });
//                }
//                catch
//                {
//                    // Remove the newly uploaded file if DB save fails.
//                    if (System.IO.File.Exists(physicalPath))
//                    {
//                        System.IO.File.Delete(physicalPath);
//                    }

//                    throw;
//                }
//            }
//            catch (Exception ex)
//            {
//                _logErrorService.LogError(
//                    ex,
//                    "Admin/UploadImage",
//                    "POST",
//                    0,
//                    Request.UserHostAddress,
//                    "Image Upload"
//                );

//                Response.StatusCode = 500;

//                return Json(new
//                {
//                    Success = false,
//                    Message = "The image could not be saved."
//                });
//            }
//        }
//    }
//}
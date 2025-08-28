using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public UploadController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // Display the file upload form
        public IActionResult Index()
        {
            return View(new FileUploadModel());
        }

        // Handle file upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                {
                    // Upload to Azure Blob Storage
                    var fileName = await _storageService.UploadBlobAsync("payment-proofs", model.ProofOfPayment);

                    // Upload to Azure File Share
                    await _storageService.UploadFileShareAsync("contracts", "payments", model.ProofOfPayment);

                    TempData["Success"] = $"File uploaded successfully! File name: {fileName}";

                    // Reset the form for a new upload
                    return View(new FileUploadModel());
                }
                else
                {
                    ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
            }

            return View(model);
        }
    }
}

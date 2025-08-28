using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;
        private const string TableName = "Products";

        public ProductController(IAzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _storageService.GetAllEntitiesAsync<Product>(TableName);
                return View(products.OrderBy(p => p.ProductName).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products");
                TempData["Error"] = "Failed to load products.";
                return View(new List<Product>());
            }
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            ViewBag.JordanTypes = GetProductTypes();
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (product.Price <= 0)
                ModelState.AddModelError("Price", "Price must be greater than 0.");

            if (!ModelState.IsValid)
            {
                ViewBag.JordanTypes = GetProductTypes(product);
                return View(product);
            }

            try
            {
                product.PartitionKey = product.ProductType.ToString();
                product.RowKey = Guid.NewGuid().ToString();

                if (imageFile != null && imageFile.Length > 0)
                    product.ImageUrl = await _storageService.UploadBlobAsync("product-images", imageFile);

                await _storageService.AddEntityAsync(TableName, product);
                TempData["Success"] = $"Product '{product.ProductName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                ViewBag.JordanTypes = GetProductTypes(product);
                return View(product);
            }
        }

        // GET: Product/Edit
        public async Task<IActionResult> Edit(string id, string partitionKey)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(partitionKey))
                return NotFound();

            var product = await _storageService.GetEntityAsync<Product>(TableName, partitionKey, id);
            if (product == null) return NotFound();

            ViewBag.JordanTypes = GetProductTypes(product);
            return View(product);
        }

        // POST: Product/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (product.Price <= 0)
                ModelState.AddModelError("Price", "Price must be greater than 0.");

            if (!ModelState.IsValid)
            {
                ViewBag.JordanTypes = GetProductTypes(product);
                return View(product);
            }

            try
            {
                var original = await _storageService.GetEntityAsync<Product>(TableName, product.PartitionKey, product.RowKey);
                if (original == null) return NotFound();

                original.ProductName = product.ProductName;
                original.Description = product.Description;
                original.Price = product.Price;
                original.StockAvailable = product.StockAvailable;
                original.ProductType = product.ProductType;

                if (imageFile != null && imageFile.Length > 0)
                    original.ImageUrl = await _storageService.UploadBlobAsync("product-images", imageFile);

                await _storageService.UpdateEntityAsync(TableName, original);
                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                ViewBag.JordanTypes = GetProductTypes(product);
                return View(product);
            }
        }

        // POST: Product/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id, string partitionKey)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(partitionKey))
                return NotFound();

            try
            {
                await _storageService.DeleteEntityAsync(TableName, partitionKey, id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private List<SelectListItem> GetProductTypes(Product product = null)
        {
            return Enum.GetValues(typeof(JordanType))
                       .Cast<JordanType>()
                       .Select(type => new SelectListItem
                       {
                           Value = type.ToString(),
                           Text = type.ToString(),
                           Selected = product != null && product.ProductType == type
                       }).ToList();
        }
    }
}

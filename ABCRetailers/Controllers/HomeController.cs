using System.Diagnostics;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IAzureStorageService storageService, ILogger<HomeController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // GET: Home/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                // Fetch all products directly from the Products table
                var allProducts = await _storageService.GetAllEntitiesAsync<Product>("Products");

                // Fetch customers and orders using updated table names
                var customers = await _storageService.GetAllEntitiesAsync<Customer>("Customers");
                var orders = await _storageService.GetAllEntitiesAsync<Order>("Orders");

                // Build the Home page view model
                var viewModel = new HomeViewModel
                {
                    FeaturedProducts = allProducts.Take(5).ToList(),
                    ProductCount = allProducts.Count(),
                    CustomerCount = customers.Count(),
                    OrderCount = orders.Count()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the error and show a fallback view model
                _logger.LogError(ex, "Error loading Home page data.");
                TempData["Error"] = $"Failed to load data: {ex.Message}";
                return View(new HomeViewModel());
            }
        }

        // POST: Home/InitializeStorage
        [HttpPost]
        public async Task<IActionResult> InitializeStorage()
        {
            try
            {
                // Trigger a storage call to ensure tables/containers exist using updated table name
                await _storageService.GetAllEntitiesAsync<Customer>("Customers");
                TempData["Success"] = "Azure Storage initialized successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to initialize storage: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Home/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}

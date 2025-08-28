using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ABCRetailers.Models;
using ABCRetailers.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private const string TableName = "Customers"; 

        public CustomerController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // List all customers
        public async Task<IActionResult> Index()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>(TableName);
            return View(customers);
        }

        // Show create form
        public IActionResult Create()
        {
            ViewBag.Provinces = GetProvinces();
            return View(new Customer());
        }

        // Handle create form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = GetProvinces();
                return View(customer);
            }

            try
            {
                customer.PartitionKey = customer.Province;
                customer.RowKey = Guid.NewGuid().ToString();
                await _storageService.AddEntityAsync(TableName, customer);
                TempData["Success"] = "Customer created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                ViewBag.Provinces = GetProvinces();
                return View(customer);
            }
        }

        // Show edit form
        public async Task<IActionResult> Edit(string partitionKey, string id)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(id))
                return NotFound();

            var customer = await _storageService.GetEntityAsync<Customer>(TableName, partitionKey, id);
            if (customer == null) return NotFound();

            ViewBag.Provinces = GetProvinces();
            return View(customer);
        }

        // Handle edit form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = GetProvinces();
                return View(customer);
            }

            try
            {
                customer.PartitionKey = customer.Province;
                await _storageService.UpdateEntityAsync(TableName, customer);
                TempData["Success"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating customer: {ex.Message}");
                ViewBag.Provinces = GetProvinces();
                return View(customer);
            }
        }

        // Delete a customer
        [HttpPost]
        public async Task<IActionResult> Delete(string partitionKey, string id)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                await _storageService.DeleteEntityAsync(TableName, partitionKey, id);
                TempData["Success"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting customer: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        private List<SelectListItem> GetProvinces()
        {
            return new List<SelectListItem>
            {
                new SelectListItem("Eastern Cape", "Eastern Cape"),
                new SelectListItem("Free State", "Free State"),
                new SelectListItem("Gauteng", "Gauteng"),
                new SelectListItem("KwaZulu-Natal", "KwaZulu-Natal"),
                new SelectListItem("Limpopo", "Limpopo"),
                new SelectListItem("Mpumalanga", "Mpumalanga"),
                new SelectListItem("Northern Cape", "Northern Cape"),
                new SelectListItem("North West", "North West"),
                new SelectListItem("Western Cape", "Western Cape")
            };
        }
    }
}

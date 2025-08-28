using ABCRetailers.Models.ViewModels;
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Linq;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private const string TableName = "Orders";

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // Display all orders
        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>(TableName);
            return View(orders);
        }

        // Display order creation form with dropdowns for customers and products
        public async Task<IActionResult> Create()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>("Customers");
            var products = await _storageService.GetAllEntitiesAsync<Product>("Products");

            var viewModel = new OrderCreateViewModel
            {
                Customers = customers.OrderBy(c => c.Name).ToList(),
                Products = products.OrderBy(p => p.ProductName).ToList()
            };

            return View(viewModel);
        }

        // Handle create order form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            try
            {
                var allCustomers = await _storageService.GetAllEntitiesAsync<Customer>("Customers");
                var allProducts = await _storageService.GetAllEntitiesAsync<Product>("Products");

                var customer = allCustomers.FirstOrDefault(c => c.RowKey == model.CustomerId);
                var product = allProducts.FirstOrDefault(p => p.RowKey == model.ProductId);

                if (customer == null || product == null)
                {
                    ModelState.AddModelError("", "Invalid customer or product selected.");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                if (product.StockAvailable < model.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                // Create new order entity with PartitionKey and RowKey
                var order = new Order
                {
                    PartitionKey = "Orders",                   
                    RowKey = Guid.NewGuid().ToString(),       
                    CustomerId = model.CustomerId,
                    Username = customer.Username,
                    ProductId = model.ProductId,
                    ProductName = product.ProductName,
                    OrderDate = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc), 
                    Quantity = model.Quantity,
                    UnitPrice = product.Price,
                    Status = "Pending"
                };

                await _storageService.AddEntityAsync(TableName, order);

                // Update product stock
                product.StockAvailable -= model.Quantity;
                await _storageService.UpdateEntityAsync("Products", product);

                // Send message to order queue
                var orderMessage = new
                {
                    OrderId = order.RowKey,
                    CustomerId = order.CustomerId,
                    CustomerName = $"{customer.Name} {customer.Surname}",
                    ProductName = product.ProductName,
                    Quantity = order.Quantity,
                    TotalPrice = order.UnitPrice * order.Quantity,
                    OrderDate = order.OrderDate,
                    Status = order.Status
                };

                // queue name updated here
                await _storageService.SendMessageAsync("notificationqueue", JsonSerializer.Serialize(orderMessage));

                TempData["Success"] = "Order created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                await PopulateDropdowns(model);
                return View(model);
            }
        }

        // Display order details
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var order = await _storageService.GetEntityAsync<Order>(TableName, "Orders", id);
            if (order == null) return NotFound();

            return View(order);
        }

        // Display edit form for an existing order
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var order = await _storageService.GetEntityAsync<Order>(TableName, "Orders", id);
            if (order == null) return NotFound();

            return View(order);
        }

        // Handle edit order form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (!ModelState.IsValid) return View(order);

            try
            {
                await _storageService.UpdateEntityAsync(TableName, order);
                TempData["Success"] = "Order updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                return View(order);
            }
        }

        // Delete an order
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync(TableName, "Orders", id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to repopulate dropdowns in case of errors
        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>("Customers");
            var products = await _storageService.GetAllEntitiesAsync<Product>("Products");

            model.Customers = customers.OrderBy(c => c.Name).ToList();
            model.Products = products.OrderBy(p => p.ProductName).ToList();
        }
    }
}

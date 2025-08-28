using ABCRetailers.Models;
using System;
using System.Collections.Generic;

namespace ABCRetailers.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        // Form input fields
        public string CustomerId { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Default order status
        public string Status { get; set; } = "Pending";  

        // Dropdown lists
        public IEnumerable<Customer> Customers { get; set; }
        public IEnumerable<Product> Products { get; set; }
    }
}

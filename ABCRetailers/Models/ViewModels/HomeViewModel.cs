using ABCRetailers.Models;
using System.Collections.Generic;

namespace ABCRetailers.Models.ViewModels
{
    public class HomeViewModel
    {
        // List of featured products
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();

        // Dashboard counts
        public int ProductCount { get; set; }
        public int CustomerCount { get; set; }
        public int OrderCount { get; set; }
    }
}

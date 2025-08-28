using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    // Enum for Jordan types
    public enum JordanType
    {
        Jordan1 = 1,
        Jordan2 = 2,
        Jordan3 = 3,
        Jordan4 = 4
    }

    public class Product : ITableEntity
    {
        // ITableEntity properties
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Product properties
        [Display(Name = "Product ID")]
        public string ProductId => RowKey;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Display(Name = "Price")]
        public double Price { get; set; }

        [Required]
        [Display(Name = "Stock Available")]
        public int StockAvailable { get; set; }

        [Display(Name = "Stock Value")]
        public double StockValue => Price * StockAvailable;

        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product Type")]
        public JordanType ProductType { get; set; } // enum used instead of int
    }
}

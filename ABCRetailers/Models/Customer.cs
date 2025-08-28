using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class Customer : ITableEntity
    {
        // PartitionKey is now tied to Province
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name = "Customer ID")]
        public string CustomerId => RowKey;

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping address is required")]
        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Province is required")]
        [Display(Name = "Province")]
        public string Province
        {
            get => PartitionKey;
            set => PartitionKey = value; // Automatically sets PartitionKey
        }
    }
}

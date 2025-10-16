using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DataTransferObject.VehicleDTO
{
    public class CreateVehicleRequest
    {
        [Required(ErrorMessage = "Category ID is required")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Color is required")]
        [StringLength(50, ErrorMessage = "Color cannot exceed 50 characters")]
        public string Color { get; set; } = null!;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Manufacture date is required")]
        public DateOnly ManufactureDate { get; set; }

        [Required(ErrorMessage = "Model is required")]
        [StringLength(100, ErrorMessage = "Model cannot exceed 100 characters")]
        public string Model { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Version cannot exceed 50 characters")]
        public string? Version { get; set; }

        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        public string? Image { get; set; }
    }
}
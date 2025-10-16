using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DataTransferObject.VehicleDTO
{
    public class VehicleResponse
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Color { get; set; } = null!;
        public decimal Price { get; set; }
        public DateOnly ManufactureDate { get; set; }
        public string Model { get; set; } = null!;
        public string? Version { get; set; }
        public string? Image { get; set; }
        public bool IsDeleted { get; set; }
    }
}
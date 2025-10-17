using System.ComponentModel.DataAnnotations;

namespace Services.DataTransferObject.InventoryDTO
{
    public class InventoryResponse
    {
        public int VehicleId { get; set; }
        public int DealerId { get; set; }
        public int Quantity { get; set; }
        
        // Vehicle information
        public string VehicleModel { get; set; } = string.Empty;
        public string VehicleColor { get; set; } = string.Empty;
        public string? VehicleVersion { get; set; }
        public decimal VehiclePrice { get; set; }
        public DateOnly VehicleManufactureDate { get; set; }
        public string? VehicleImage { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        
        // Dealer information
        public string DealerName { get; set; } = string.Empty;
        public string DealerAddress { get; set; } = string.Empty;
    }
}
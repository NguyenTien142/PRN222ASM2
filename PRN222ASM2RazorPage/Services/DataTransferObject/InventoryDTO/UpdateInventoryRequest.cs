using System.ComponentModel.DataAnnotations;

namespace Services.DataTransferObject.InventoryDTO
{
    public class UpdateInventoryRequest
    {
        [Required]
        public int VehicleId { get; set; }
        
        [Required]
        public int DealerId { get; set; }
        
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative number")]
        public int Quantity { get; set; }
    }
}
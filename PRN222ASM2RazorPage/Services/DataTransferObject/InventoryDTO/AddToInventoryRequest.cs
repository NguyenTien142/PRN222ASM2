using System.ComponentModel.DataAnnotations;

namespace Services.DataTransferObject.InventoryDTO
{
    public class AddToInventoryRequest
    {
        [Required]
        public int VehicleId { get; set; }
        
        [Required]
        public int DealerId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }
    }
}
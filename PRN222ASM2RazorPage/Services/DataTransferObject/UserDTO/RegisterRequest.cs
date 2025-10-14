using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DataTransferObject.UserDTO
{
    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = null!;

        [Required]
        public int RoleId { get; set; }

        // Customer specific fields
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerAddress { get; set; }

        // Dealer specific fields
        public string? DealerName { get; set; }
        public string? DealerAddress { get; set; }
        public int? DealerQuantity { get; set; }
    }
}

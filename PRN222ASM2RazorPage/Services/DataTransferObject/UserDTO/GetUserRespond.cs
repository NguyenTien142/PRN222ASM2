using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DataTransferObject.UserDTO
{
    public class GetUserRespond
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public bool IsDeleted { get; set; }

        // Customer information (if user is customer)
        public CustomerInfo? Customer { get; set; }

        // Dealer information (if user is dealer)
        public DealerInfo? Dealer { get; set; }
    }

    public class CustomerInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Address { get; set; } = null!;
    }

    public class DealerInfo
    {
        public int Id { get; set; }
        public string DealerName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public int Quantity { get; set; }
    }
}

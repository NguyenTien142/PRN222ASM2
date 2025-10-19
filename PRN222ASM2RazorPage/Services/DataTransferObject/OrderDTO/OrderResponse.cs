using Services.DataTransferObject.UserDTO;
using Services.DataTransferObject.VehicleDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DataTransferObject.OrderDTO
{
    public class OrderResponse
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int DealerId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;

        // Navigation properties
        public CustomerResponse? Customer { get; set; }
        public DealerResponse? Dealer { get; set; }
        public List<OrderVehicleResponse>? OrderVehicles { get; set; }
    }

    public class OrderVehicleResponse
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int VehicleId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Navigation properties
        public VehicleResponse? Vehicle { get; set; }
    }

    public class CustomerResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class DealerResponse
    {
        public int Id { get; set; }
        public string DealerName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
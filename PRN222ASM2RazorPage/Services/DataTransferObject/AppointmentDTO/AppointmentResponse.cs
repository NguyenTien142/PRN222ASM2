using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DataTransferObject.AppointmentDTO
{
    public class AppointmentResponse
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int VehicleId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = null!;

        // Related entity data
        public string CustomerName { get; set; } = null!;
        public string CustomerPhone { get; set; } = null!;
        public string CustomerAddress { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        
        public string VehicleModel { get; set; } = null!;
        public string VehicleColor { get; set; } = null!;
        public string? VehicleVersion { get; set; }
        public decimal VehiclePrice { get; set; }
        public string VehicleCategoryName { get; set; } = null!;
        public string? VehicleImage { get; set; }
    }
}
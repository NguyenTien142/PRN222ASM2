using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DataTransferObject.AppointmentDTO
{
    public class CreateAppointmentRequest
    {
        [Required(ErrorMessage = "Customer ID is required")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Vehicle ID is required")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        public DateTime AppointmentDate { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DataTransferObject.AppointmentDTO
{
    public class UpdateAppointmentRequest
    {
        [Required(ErrorMessage = "Appointment ID is required")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        public DateTime AppointmentDate { get; set; }
    }
}
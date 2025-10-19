using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Helpper;
using Services.Interfaces;
using Services.DataTransferObject.AppointmentDTO;

namespace ElectricVehicleDealerManagermentSystem.Pages.Appointment
{
    public class updateModel : BasePageModel
    {
        private readonly IAppointmentServices _appointmentServices;

        public updateModel(IUserServices userServices, IAppointmentServices appointmentServices)
            : base(userServices)
        {
            _appointmentServices = appointmentServices;
        }

        // Properties for user info
        public int? UserId { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        // Properties for appointment data
        public AppointmentResponse? CurrentAppointment { get; set; }

        // Properties for reschedule form
        [BindProperty]
        public int AppointmentId { get; set; }

        [BindProperty]
        public DateTime NewAppointmentDate { get; set; }

        // Time slot properties
        public List<DateTime> AvailableTimeSlots { get; set; } = new List<DateTime>();
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        // Properties for messages
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int? appointmentId)
        {
            // Check if user is logged in
            UserId = HttpContext.Session.GetInt32("UserId");
            if (!UserId.HasValue)
            {
                return RedirectToPage("/Credential/Login");
            }

            // Check if user is a customer
            var roleName = HttpContext.Session.GetString("RoleName")?.ToLower();
            if (roleName != "customer")
            {
                TempData["ErrorMessage"] = "You don't have permission to access this page.";
                return RedirectToPage("/Index");
            }

            // Get customer information
            CustomerId = HttpContext.Session.GetInt32("CustomerId");
            CustomerName = HttpContext.Session.GetString("CustomerName") ?? "Unknown Customer";

            if (!CustomerId.HasValue)
            {
                TempData["ErrorMessage"] = "Customer information not found. Please log in again.";
                return RedirectToPage("/Credential/Login");
            }

            // Check if appointment ID is provided
            if (!appointmentId.HasValue)
            {
                TempData["ErrorMessage"] = "Appointment ID is required.";
                return RedirectToPage("/Appointment/Index");
            }

            AppointmentId = appointmentId.Value;

            // Load appointment data
            await LoadAppointmentAsync();

            if (CurrentAppointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found or you don't have permission to modify it.";
                return RedirectToPage("/Appointment/Index");
            }

            // Check if appointment can be rescheduled
            if (!CanRescheduleAppointment(CurrentAppointment.Status))
            {
                TempData["ErrorMessage"] = "This appointment cannot be rescheduled in its current status.";
                return RedirectToPage("/Appointment/Index");
            }

            // Set default values
            NewAppointmentDate = CurrentAppointment.AppointmentDate;
            SelectedDate = CurrentAppointment.AppointmentDate.Date;
            GenerateTimeSlots();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Restore user session data
            UserId = HttpContext.Session.GetInt32("UserId");
            CustomerId = HttpContext.Session.GetInt32("CustomerId");
            CustomerName = HttpContext.Session.GetString("CustomerName") ?? "Unknown Customer";

            if (!UserId.HasValue || !CustomerId.HasValue)
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToPage("/Credential/Login");
            }

            if (!ModelState.IsValid)
            {
                await LoadAppointmentAsync();
                GenerateTimeSlots();
                return Page();
            }

            try
            {
                var updateRequest = new UpdateAppointmentRequest
                {
                    Id = AppointmentId,
                    AppointmentDate = NewAppointmentDate
                };

                var result = await _appointmentServices.UpdateAppointmentAsync(updateRequest);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToPage("/Appointment/Index");
                }
                else
                {
                    ErrorMessage = result.Message;
                    await LoadAppointmentAsync();
                    GenerateTimeSlots();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while updating the appointment: " + ex.Message;
                await LoadAppointmentAsync();
                GenerateTimeSlots();
                return Page();
            }
        }

        public async Task<IActionResult> OnGetAvailableTimeSlotsAsync(DateTime date)
        {
            // Generate available time slots for the selected date
            var timeSlots = GenerateAvailableTimeSlots(date);
            
            return new JsonResult(timeSlots.Select(t => new { 
                value = t.ToString("yyyy-MM-ddTHH:mm"),
                text = t.ToString("HH:mm")
            }));
        }

        private async Task LoadAppointmentAsync()
        {
            try
            {
                var result = await _appointmentServices.GetAppointmentByIdAsync(AppointmentId);
                if (result.Success && result.Data != null)
                {
                    // Verify that the appointment belongs to the current customer
                    if (result.Data.CustomerId == CustomerId.Value)
                    {
                        CurrentAppointment = result.Data;
                    }
                    else
                    {
                        CurrentAppointment = null;
                    }
                }
                else
                {
                    CurrentAppointment = null;
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                CurrentAppointment = null;
                ErrorMessage = "An error occurred while loading the appointment: " + ex.Message;
            }
        }

        private void GenerateTimeSlots()
        {
            AvailableTimeSlots = GenerateAvailableTimeSlots(SelectedDate);
        }

        private List<DateTime> GenerateAvailableTimeSlots(DateTime date)
        {
            var timeSlots = new List<DateTime>();
            
            // Only show slots for future dates
            if (date.Date < DateTime.Today)
                return timeSlots;

            // Business hours: 9 AM to 6 PM, every hour
            var startTime = date.Date.AddHours(9);
            var endTime = date.Date.AddHours(18);

            for (var time = startTime; time < endTime; time = time.AddHours(1))
            {
                // Skip if the time slot is in the past
                if (time <= DateTime.Now)
                    continue;

                // Skip lunch hour (12 PM - 1 PM)
                if (time.Hour == 12)
                    continue;

                timeSlots.Add(time);
            }

            return timeSlots;
        }

        // Helper methods
        public string GetStatusBadgeClass(string status)
        {
            return status.ToUpper() switch
            {
                "PENDING" => "badge bg-warning text-dark",
                "APPROVE" => "badge bg-success",
                "CANCELLED" => "badge bg-danger",
                "RUNNING" => "badge bg-info",
                "COMPLETED" => "badge bg-primary",
                "EXPIRED" => "badge bg-secondary",
                _ => "badge bg-light text-dark"
            };
        }

        public bool CanRescheduleAppointment(string status)
        {
            return status.Equals("PENDING", StringComparison.OrdinalIgnoreCase);
        }
    }
}

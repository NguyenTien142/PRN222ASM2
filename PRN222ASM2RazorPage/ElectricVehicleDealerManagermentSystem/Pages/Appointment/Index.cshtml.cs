using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Helpper;
using Services.Interfaces;
using Services.DataTransferObject.AppointmentDTO;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ElectricVehicleDealerManagermentSystem.Pages.Appointment
{
    public class IndexModel : BasePageModel
    {
        private readonly IAppointmentServices _appointmentServices;

        public IndexModel(IUserServices userServices, IAppointmentServices appointmentServices)
            : base(userServices)
        {
            _appointmentServices = appointmentServices;
        }

        // Properties for appointment data
        public IList<AppointmentResponse> Appointments { get; set; } = new List<AppointmentResponse>();
        
        // Properties for messages
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        
        // Properties for user info
        public int? UserId { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        // Properties for filters
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        // Select lists
        public SelectList StatusSelectList { get; set; } = new SelectList(new List<object>());

        public async Task<IActionResult> OnGetAsync()
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

            await LoadDataAsync();

            // Check for temp data messages
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"].ToString()!;
            }
            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"].ToString()!;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAppointmentAsync(int appointmentId)
        {
            try
            {
                var result = await _appointmentServices.CancelAppointmentAsync(appointmentId);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while cancelling the appointment: " + ex.Message;
            }

            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            await LoadAppointmentsAsync();
            LoadSelectLists();
        }

        private async Task LoadAppointmentsAsync()
        {
            if (!UserId.HasValue) return;

            try
            {
                // The appointments service will automatically check and update expired appointments
                var result = await _appointmentServices.GetAppointmentsByUserIdAsync(UserId.Value);
                if (result.Success && result.Data != null)
                {
                    var appointments = result.Data.ToList();

                    // Apply filters
                    if (!string.IsNullOrEmpty(StatusFilter))
                    {
                        appointments = appointments.Where(a => a.Status.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    if (StartDate.HasValue)
                    {
                        appointments = appointments.Where(a => a.AppointmentDate >= StartDate.Value).ToList();
                    }

                    if (EndDate.HasValue)
                    {
                        appointments = appointments.Where(a => a.AppointmentDate <= EndDate.Value).ToList();
                    }

                    Appointments = appointments;
                }
                else
                {
                    ErrorMessage = result.Message;
                    Appointments = new List<AppointmentResponse>();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while loading appointments: " + ex.Message;
                Appointments = new List<AppointmentResponse>();
            }
        }

        private void LoadSelectLists()
        {
            // Status filter options
            var statusItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Statuses" },
                new SelectListItem { Value = "PENDING", Text = "Pending" },
                new SelectListItem { Value = "APPROVE", Text = "Approved" },
                new SelectListItem { Value = "CANCELLED", Text = "Cancelled" },
                new SelectListItem { Value = "RUNNING", Text = "Running" },
                new SelectListItem { Value = "COMPLETED", Text = "Completed" },
                new SelectListItem { Value = "EXPIRED", Text = "Expired" }
            };

            StatusSelectList = new SelectList(statusItems, "Value", "Text", StatusFilter);
        }

        // Helper methods for the view
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

        public bool CanCancelAppointment(string status)
        {
            return status.Equals("PENDING", StringComparison.OrdinalIgnoreCase);
        }

        public bool CanRescheduleAppointment(string status)
        {
            return status.Equals("PENDING", StringComparison.OrdinalIgnoreCase);
        }
    }
}

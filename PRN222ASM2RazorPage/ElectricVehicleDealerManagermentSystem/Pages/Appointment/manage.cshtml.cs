using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Helpper;
using Services.Interfaces;
using Services.DataTransferObject.AppointmentDTO;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ElectricVehicleDealerManagermentSystem.Pages.Appointment
{
    public class manageModel : BasePageModel
    {
        private readonly IAppointmentServices _appointmentServices;

        public manageModel(IUserServices userServices, IAppointmentServices appointmentServices)
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
        public int? DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;

        // Properties for filters
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string CustomerNameFilter { get; set; } = string.Empty;

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

            // Check if user is a dealer
            var roleName = HttpContext.Session.GetString("RoleName")?.ToLower();
            if (roleName != "dealer")
            {
                TempData["ErrorMessage"] = "You don't have permission to access this page.";
                return RedirectToPage("/Index");
            }

            // Get dealer information
            DealerId = HttpContext.Session.GetInt32("DealerId");
            DealerName = HttpContext.Session.GetString("DealerName") ?? "Unknown Dealer";

            if (!DealerId.HasValue)
            {
                TempData["ErrorMessage"] = "Dealer information not found. Please log in again.";
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

        public async Task<IActionResult> OnPostApproveAppointmentAsync(int appointmentId)
        {
            try
            {
                var result = await _appointmentServices.ApproveAppointmentAsync(appointmentId);

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
                TempData["ErrorMessage"] = "An error occurred while approving the appointment: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostStartAppointmentAsync(int appointmentId)
        {
            try
            {
                var result = await _appointmentServices.StartAppointmentAsync(appointmentId);

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
                TempData["ErrorMessage"] = "An error occurred while starting the appointment: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompleteAppointmentAsync(int appointmentId)
        {
            try
            {
                var result = await _appointmentServices.CompleteAppointmentAsync(appointmentId);

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
                TempData["ErrorMessage"] = "An error occurred while completing the appointment: " + ex.Message;
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
            try
            {
                // Get all appointments without pagination
                var filter = new AppointmentFilterRequest
                {
                    Status = StatusFilter,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    PageIndex = 0, // Use 0-based for filter
                    PageSize = 1000 // Large page size to get all appointments
                };

                var result = await _appointmentServices.GetAllAppointmentsAsync(filter);
                if (result.Success && result.Data != null)
                {
                    var appointments = result.Data.Items.ToList();

                    // Apply customer name filter if provided
                    if (!string.IsNullOrEmpty(CustomerNameFilter))
                    {
                        appointments = appointments.Where(a => 
                            a.CustomerName.Contains(CustomerNameFilter, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    Appointments = appointments;
                }
                else
                {
                    ErrorMessage = result?.Message ?? "Failed to load appointments";
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

        public bool CanApproveAppointment(string status)
        {
            return status.Equals("PENDING", StringComparison.OrdinalIgnoreCase);
        }

        public bool CanStartAppointment(string status, DateTime appointmentDate)
        {
            if (!status.Equals("APPROVE", StringComparison.OrdinalIgnoreCase))
                return false;

            // Can start up to 30 minutes before appointment time and up to 2 hours after
            var currentTime = DateTime.Now;
            var startWindow = appointmentDate.AddMinutes(-30);
            var endWindow = appointmentDate.AddHours(2);

            return currentTime >= startWindow && currentTime <= endWindow;
        }

        public bool CanCompleteAppointment(string status)
        {
            return status.Equals("RUNNING", StringComparison.OrdinalIgnoreCase);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Services.DataTransferObject.AppointmentDTO;
using ElectricVehicleDealerManagermentSystem.Helpper;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public IList<AppointmentResponse> Appointments { get; set; } = new List<AppointmentResponse>();
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;
        public string UserRole { get; private set; }
        public int? CurrentUserId { get; private set; }
        public int? CurrentCustomerId { get; private set; }
        public bool IsUserLoggedIn { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentUserId = HttpContext.Session.GetInt32("UserId");
            CurrentCustomerId = HttpContext.Session.GetInt32("CustomerId");
            UserRole = HttpContext.Session.GetString("RoleName")?.ToLower()!;
            IsUserLoggedIn = CurrentUserId.HasValue;

            if (!IsUserLoggedIn)
            {
                return RedirectToPage("/Credential/Login", new { returnUrl = "/Appointment/Index" });
            }

            await LoadAppointmentsAsync();
            return Page();
        }

        private async Task LoadAppointmentsAsync()
        {
            try
            {
                if (UserRole == "customer" && CurrentCustomerId.HasValue)
                {
                    var result = await _appointmentServices.GetAppointmentsByCustomerAsync(CurrentCustomerId.Value);
                    if (result.Success && result.Data != null)
                        Appointments = new List<AppointmentResponse>(result.Data);
                    else
                        ErrorMessage = result.Message ?? "Failed to load your appointments.";
                }
                else if (UserRole == "dealer" || UserRole == "admin")
                {
                    var result = await _appointmentServices.GetAllAppointmentsAsync();
                    if (result.Success && result.Data != null)
                        Appointments = new List<AppointmentResponse>(result.Data);
                    else
                        ErrorMessage = result.Message ?? "Failed to load appointments.";
                }
                else
                {
                    ErrorMessage = "You don't have permission to view appointments.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred while loading appointments: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            try
            {
                
                // Ko implement thi co cl :)
                await LoadAppointmentsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred while cancelling the appointment: {ex.Message}";
                await LoadAppointmentsAsync();
                return Page();
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Helpper;
using Services.Interfaces;
using Services.DataTransferObject.AppointmentDTO;
using Services.DataTransferObject.VehicleDTO;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ElectricVehicleDealerManagermentSystem.Pages.Appointment
{
    public class createModel : BasePageModel
    {
        private readonly IAppointmentServices _appointmentServices;
        private readonly IVehicleServices _vehicleServices;

        public createModel(IUserServices userServices, IAppointmentServices appointmentServices, IVehicleServices vehicleServices)
            : base(userServices)
        {
            _appointmentServices = appointmentServices;
            _vehicleServices = vehicleServices;
        }

        // Properties for user info
        public int? UserId { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        // Properties for new appointment form
        [BindProperty]
        public CreateAppointmentRequest NewAppointment { get; set; } = new();

        // Properties for vehicle selection
        public IList<VehicleResponse> AvailableVehicles { get; set; } = new List<VehicleResponse>();
        public SelectList VehicleSelectList { get; set; } = new SelectList(new List<object>());

        // Time slot properties
        public List<DateTime> AvailableTimeSlots { get; set; } = new List<DateTime>();
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        // Properties for messages
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

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

            // Initialize new appointment with customer ID
            NewAppointment.CustomerId = CustomerId.Value;

            await LoadDataAsync();

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

            NewAppointment.CustomerId = CustomerId.Value;

            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            try
            {
                var result = await _appointmentServices.CreateAppointmentAsync(NewAppointment);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToPage("/Appointment/Index");
                }
                else
                {
                    ErrorMessage = result.Message;
                    await LoadDataAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while creating the appointment: " + ex.Message;
                await LoadDataAsync();
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

        private async Task LoadDataAsync()
        {
            await LoadVehiclesAsync();
            GenerateTimeSlots();
        }

        private async Task LoadVehiclesAsync()
        {
            try
            {
                var result = await _vehicleServices.GetAllVehiclesAsync();
                if (result.Success && result.Data != null)
                {
                    AvailableVehicles = result.Data.Where(v => !v.IsDeleted).ToList();
                    
                    var vehicleItems = AvailableVehicles.Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = $"{v.Model} - {v.Color} ({v.CategoryName}) - ${v.Price:N0}"
                    }).ToList();

                    vehicleItems.Insert(0, new SelectListItem { Value = "", Text = "-- Select Vehicle --" });
                    VehicleSelectList = new SelectList(vehicleItems, "Value", "Text");
                }
                else
                {
                    AvailableVehicles = new List<VehicleResponse>();
                    VehicleSelectList = new SelectList(new List<SelectListItem>());
                }
            }
            catch (Exception)
            {
                AvailableVehicles = new List<VehicleResponse>();
                VehicleSelectList = new SelectList(new List<SelectListItem>());
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
    }
}

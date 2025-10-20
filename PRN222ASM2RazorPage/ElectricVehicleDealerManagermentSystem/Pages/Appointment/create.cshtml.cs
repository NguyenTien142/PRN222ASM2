using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Helpper;
using Services.Interfaces;
using Services.DataTransferObject.AppointmentDTO;
using Services.DataTransferObject.VehicleDTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using ElectricVehicleDealerManagermentSystem.SignalR;

namespace ElectricVehicleDealerManagermentSystem.Pages.Appointment
{
    public class createModel : BasePageModel
    {
        private readonly IAppointmentServices _appointmentServices;
        private readonly IVehicleServices _vehicleServices;
        private readonly IHubContext<SignalRHub> _hubContext;

        public createModel(IUserServices userServices, IAppointmentServices appointmentServices, IVehicleServices vehicleServices, IHubContext<SignalRHub> hubContext)
            : base(userServices)
        {
            _appointmentServices = appointmentServices;
            _vehicleServices = vehicleServices;
            _hubContext = hubContext;
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
        public Dictionary<DateTime, List<DateTime>> MonthlyAvailableSlots { get; set; } = new Dictionary<DateTime, List<DateTime>>();
        public DateTime CurrentMonth { get; set; } = DateTime.Today;

        // Properties for messages
        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsVehiclePreSelected { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(int? vehicleId = null)
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

            // Pre-select vehicle if provided in query string
            if (vehicleId.HasValue && vehicleId.Value > 0)
            {
                NewAppointment.VehicleId = vehicleId.Value;
                IsVehiclePreSelected = true;
            }

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
                    // Send real-time notification to manage appointment page
                    await _hubContext.Clients.All.SendAsync("LoadAllItems");
                    
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
            var timeSlots = await GenerateAvailableTimeSlotsAsync(date);
            
            return new JsonResult(timeSlots.Select(t => new { 
                value = t.ToString("yyyy-MM-ddTHH:mm"),
                text = t.ToString("HH:mm")
            }));
        }

        public async Task<IActionResult> OnGetAvailableTimeSlotsForVehicleAsync(DateTime date, int vehicleId)
        {
            // Generate available time slots for the selected date and vehicle
            var timeSlots = await GenerateAvailableTimeSlotsAsync(date, vehicleId);
            
            return new JsonResult(timeSlots.Select(t => new { 
                value = t.ToString("yyyy-MM-ddTHH:mm"),
                text = t.ToString("HH:mm")
            }));
        }

        public async Task<IActionResult> OnGetMonthlyCalendarAsync(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var calendarData = new List<object>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var availableSlots = await GenerateAvailableTimeSlotsAsync(date);
                calendarData.Add(new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    dayOfMonth = date.Day,
                    isToday = date.Date == DateTime.Today,
                    isPast = date.Date < DateTime.Today,
                    hasSlots = availableSlots.Any(),
                    slotsCount = availableSlots.Count,
                    slots = availableSlots.Select(s => new
                    {
                        time = s.ToString("HH:mm"),
                        value = s.ToString("yyyy-MM-ddTHH:mm")
                    }).ToList()
                });
            }

            return new JsonResult(new
            {
                year = year,
                month = month,
                monthName = startDate.ToString("MMMM yyyy"),
                days = calendarData
            });
        }

        private async Task LoadDataAsync()
        {
            await LoadVehiclesAsync();
            await GenerateTimeSlotsAsync();
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
                        Text = $"{v.Model} - {v.Color} ({v.CategoryName}) - ${v.Price:N0}",
                        Selected = v.Id == NewAppointment.VehicleId // Pre-select if this is the vehicle from query string
                    }).ToList();

                    vehicleItems.Insert(0, new SelectListItem { Value = "", Text = "-- Select Vehicle --" });
                    VehicleSelectList = new SelectList(vehicleItems, "Value", "Text", NewAppointment.VehicleId > 0 ? NewAppointment.VehicleId.ToString() : "");
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

        private async Task GenerateTimeSlotsAsync()
        {
            AvailableTimeSlots = await GenerateAvailableTimeSlotsAsync(SelectedDate);
        }

        private void GenerateTimeSlots()
        {
            // Synchronous wrapper for backward compatibility
            AvailableTimeSlots = GenerateAvailableTimeSlots(SelectedDate);
        }

        private async Task<List<DateTime>> GenerateAvailableTimeSlotsAsync(DateTime date, int? vehicleId = null)
        {
            var timeSlots = new List<DateTime>();
            
            // Only show slots for future dates
            if (date.Date < DateTime.Today)
                return timeSlots;

            // Business hours: 9 AM to 6 PM, every hour
            var startTime = date.Date.AddHours(9);
            var endTime = date.Date.AddHours(18);

            // Get existing appointments for the date (and optionally for specific vehicle)
            var existingAppointments = await GetExistingAppointmentsForDateAsync(date, vehicleId);

            for (var time = startTime; time < endTime; time = time.AddHours(1))
            {
                // Skip if the time slot is in the past (for today only)
                if (date.Date == DateTime.Today && time <= DateTime.Now.AddMinutes(30))
                    continue;

                // Skip lunch hour (12 PM - 1 PM)
                if (time.Hour == 12)
                    continue;

                // Skip weekends after 4 PM (optional business rule)
                if ((date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) && time.Hour >= 16)
                    continue;

                // Check if this time slot is already booked
                bool isBooked = existingAppointments.Any(apt => 
                    apt.AppointmentDate.Date == time.Date && 
                    apt.AppointmentDate.Hour == time.Hour);

                if (!isBooked)
                {
                    timeSlots.Add(time);
                }
            }

            return timeSlots;
        }

        private List<DateTime> GenerateAvailableTimeSlots(DateTime date)
        {
            // Synchronous wrapper for backward compatibility
            return GenerateAvailableTimeSlotsAsync(date).GetAwaiter().GetResult();
        }

        private async Task<List<AppointmentResponse>> GetExistingAppointmentsForDateAsync(DateTime date, int? vehicleId = null)
        {
            try
            {
                var filter = new AppointmentFilterRequest
                {
                    StartDate = date.Date,
                    EndDate = date.Date.AddDays(1).AddSeconds(-1), // End of the day
                    VehicleId = vehicleId, // Filter by vehicle if provided
                    PageIndex = 0,
                    PageSize = 100 // Get all appointments for the day
                };

                var result = await _appointmentServices.GetAllAppointmentsAsync(filter);
                
                if (result.Success && result.Data != null)
                {
                    // Only return appointments that would block the time slot
                    // Exclude cancelled, completed, and expired appointments
                    return result.Data.Items
                        .Where(apt => apt.Status == "PENDING" || 
                                     apt.Status == "APPROVE" || 
                                     apt.Status == "RUNNING")
                        .ToList();
                }
                
                return new List<AppointmentResponse>();
            }
            catch (Exception ex)
            {
                // Log the exception for debugging (in production, use proper logging)
                System.Diagnostics.Debug.WriteLine($"Error getting existing appointments: {ex.Message}");
                
                // If there's an error getting appointments, return empty list (fail safe)
                return new List<AppointmentResponse>();
            }
        }
    }
}

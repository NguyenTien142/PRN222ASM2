using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Helpper;
using Services.Interfaces;
using Services.DataTransferObject.InventoryDTO;
using Services.DataTransferObject.VehicleDTO;
using Services.DataTransferObject.VehicleCategoryDTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using ElectricVehicleDealerManagermentSystem.SignalR;

namespace ElectricVehicleDealerManagermentSystem.Pages.Inventory
{
    public class CreateModel : BasePageModel
    {
        private readonly IDealerServices _dealerServices;
        private readonly IVehicleServices _vehicleServices;
        private readonly IVehicleCategoryServices _categoryServices;
        private readonly IHubContext<SignalRHub> _hubContext;

        public CreateModel(IUserServices userServices, IDealerServices dealerServices, 
            IVehicleServices vehicleServices, IVehicleCategoryServices categoryServices, 
            IHubContext<SignalRHub> hubContext)
            : base(userServices)
        {
            _dealerServices = dealerServices;
            _vehicleServices = vehicleServices;
            _categoryServices = categoryServices;
            _hubContext = hubContext;
        }

        [BindProperty]
        public AddToInventoryRequest InventoryInput { get; set; } = new();

        public IList<VehicleResponse> AvailableVehicles { get; set; } = new List<VehicleResponse>();
        public IList<VehicleCategoryResponse> Categories { get; set; } = new List<VehicleCategoryResponse>();
        public string ErrorMessage { get; set; } = string.Empty;
        public int? DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;

        public SelectList VehicleSelectList { get; set; } = new SelectList(new List<object>());
        public SelectList CategorySelectList { get; set; } = new SelectList(new List<object>());

        [BindProperty(SupportsGet = true)]
        public int? CategoryFilter { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Credential/Login");
            }

            // Check if user is a dealer
            var roleName = HttpContext.Session.GetString("RoleName")?.ToLower();
            if (roleName != "dealer" && roleName != "admin")
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

            // Only initialize if this is a fresh load (not a category filter change)
            if (InventoryInput.DealerId == 0)
            {
                InventoryInput.DealerId = DealerId.Value;
                InventoryInput.Quantity = 1; // Default quantity
                InventoryInput.VehicleId = 0; // No vehicle selected by default
            }

            await LoadDataAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Get dealer information from session
            DealerId = HttpContext.Session.GetInt32("DealerId");
            DealerName = HttpContext.Session.GetString("DealerName") ?? "Unknown Dealer";

            if (!DealerId.HasValue)
            {
                TempData["ErrorMessage"] = "Dealer information not found. Please log in again.";
                return RedirectToPage("/Credential/Login");
            }

            InventoryInput.DealerId = DealerId.Value;

            // Validate that a vehicle is selected
            if (InventoryInput.VehicleId <= 0)
            {
                ErrorMessage = "Please select a vehicle to add to inventory.";
                await LoadDataAsync();
                return Page();
            }

            // Validate quantity
            if (InventoryInput.Quantity <= 0)
            {
                ErrorMessage = "Quantity must be greater than 0.";
                await LoadDataAsync();
                return Page();
            }

            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            try
            {
                var result = await _dealerServices.AddToInventoryAsync(InventoryInput);
                
                if (result.Success)
                {
                    // Notify other clients about inventory update
                    await _hubContext.Clients.All.SendAsync("LoadAllItems");
                    
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToPage("./Index");
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
                ErrorMessage = "An error occurred while adding to inventory: " + ex.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        private async Task LoadDataAsync()
        {
            await LoadCategoriesAsync();
            await LoadAvailableVehiclesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var result = await _categoryServices.GetAllCategoriesAsync();
                if (result.Success && result.Data != null)
                {
                    Categories = result.Data.ToList();
                    
                    var categoryItems = Categories.Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = c.Id == CategoryFilter
                    }).ToList();

                    categoryItems.Insert(0, new SelectListItem { Value = "", Text = "All Categories" });
                    CategorySelectList = new SelectList(categoryItems, "Value", "Text", CategoryFilter?.ToString());
                }
                else
                {
                    Categories = new List<VehicleCategoryResponse>();
                    CategorySelectList = new SelectList(new List<SelectListItem>());
                }
            }
            catch (Exception)
            {
                Categories = new List<VehicleCategoryResponse>();
                CategorySelectList = new SelectList(new List<SelectListItem>());
            }
        }

        private async Task LoadAvailableVehiclesAsync()
        {
            try
            {
                var result = await _vehicleServices.GetAllVehiclesAsync();
                if (result.Success && result.Data != null)
                {
                    var vehicles = result.Data.ToList();

                    // Filter by category if selected
                    if (CategoryFilter.HasValue)
                    {
                        vehicles = vehicles.Where(v => v.CategoryId == CategoryFilter.Value).ToList();
                    }

                    AvailableVehicles = vehicles;
                    
                    var vehicleItems = AvailableVehicles.Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = $"{v.Model} - {v.Color} ({v.CategoryName}) - ${v.Price:N0}"
                    }).ToList();

                    vehicleItems.Insert(0, new SelectListItem { Value = "", Text = "-- Select Vehicle --" });
                    VehicleSelectList = new SelectList(vehicleItems, "Value", "Text", InventoryInput.VehicleId > 0 ? InventoryInput.VehicleId.ToString() : "");
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
    }
}

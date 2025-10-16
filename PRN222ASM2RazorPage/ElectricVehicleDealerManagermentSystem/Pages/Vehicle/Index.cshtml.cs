using ElectricVehicleDealerManagermentSystem.Pages.Shared;
using ElectricVehicleDealerManagermentSystem.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Services.DataTransferObject.VehicleCategoryDTO;
using Services.DataTransferObject.VehicleDTO;
using Services.Interfaces;

namespace ElectricVehicleDealerManagermentSystem.Pages.Vehicle
{
    public class IndexModel : BasePageModel
    {
        private readonly IVehicleServices _vehicleServices;
        private readonly IVehicleCategoryServices _categoryServices;
        private readonly IHubContext<SignalRHub> _hubContext;

        public IndexModel(IUserServices userServices, IVehicleServices vehicleServices, IVehicleCategoryServices categoryServices, IHubContext<SignalRHub> hubContext)
            : base(userServices)
        {
            _vehicleServices = vehicleServices;
            _categoryServices = categoryServices;
            _hubContext = hubContext;
        }

        public IList<VehicleResponse> Vehicles { get; set; } = new List<VehicleResponse>();
        public IList<VehicleCategoryResponse> Categories { get; set; } = new List<VehicleCategoryResponse>();
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowDeleted { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        public SelectList CategorySelectList { get; set; } = new SelectList(new List<object>());

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

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var result = await _vehicleServices.SoftDeleteVehicleAsync(id);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;

                    await _hubContext.Clients.All.SendAsync("LoadAllItems");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the vehicle: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRestoreAsync(int id)
        {
            try
            {
                var result = await _vehicleServices.RestoreVehicleAsync(id);
                
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
                TempData["ErrorMessage"] = "An error occurred while restoring the vehicle: " + ex.Message;
            }

            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            await LoadCategoriesAsync();
            await LoadVehiclesAsync();
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
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while loading categories: " + ex.Message;
                Categories = new List<VehicleCategoryResponse>();
                CategorySelectList = new SelectList(new List<SelectListItem>());
            }
        }

        private async Task LoadVehiclesAsync()
        {
            try
            {
                var result = await _vehicleServices.GetAllVehiclesAsync(ShowDeleted);
                if (result.Success && result.Data != null)
                {
                    var vehicles = result.Data.ToList();

                    // Apply category filter
                    if (CategoryFilter.HasValue)
                    {
                        vehicles = vehicles.Where(v => v.CategoryId == CategoryFilter.Value).ToList();
                    }

                    // Apply search filter
                    if (!string.IsNullOrEmpty(SearchTerm))
                    {
                        vehicles = vehicles.Where(v => 
                            v.Model.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                            v.Color.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(v.Version) && v.Version.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                            v.CategoryName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                        ).ToList();
                    }

                    Vehicles = vehicles;
                }
                else
                {
                    if (!string.IsNullOrEmpty(ErrorMessage))
                        ErrorMessage += " " + result.Message;
                    else
                        ErrorMessage = result.Message;
                    Vehicles = new List<VehicleResponse>();
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ErrorMessage))
                    ErrorMessage += " An error occurred while loading vehicles: " + ex.Message;
                else
                    ErrorMessage = "An error occurred while loading vehicles: " + ex.Message;
                Vehicles = new List<VehicleResponse>();
            }
        }
    }
}

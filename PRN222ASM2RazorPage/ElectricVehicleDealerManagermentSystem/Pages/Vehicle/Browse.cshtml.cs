using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Services.DataTransferObject.VehicleDTO;
using Services.DataTransferObject.VehicleCategoryDTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using ElectricVehicleDealerManagermentSystem.Helpper;

namespace ElectricVehicleDealerManagermentSystem.Pages.Vehicle
{
    public class BrowseVehicleModel : BasePageModel
    {
        private readonly IVehicleServices _vehicleServices;
        private readonly IVehicleCategoryServices _categoryServices;

        public BrowseVehicleModel(IUserServices userServices, IVehicleServices vehicleServices, IVehicleCategoryServices categoryServices)
            : base(userServices)
        {
            _vehicleServices = vehicleServices;
            _categoryServices = categoryServices;
        }

        public IList<VehicleResponse> Vehicles { get; set; } = new List<VehicleResponse>();
        public IList<VehicleCategoryResponse> Categories { get; set; } = new List<VehicleCategoryResponse>();
        public string ErrorMessage { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        public SelectList CategorySelectList { get; set; } = new SelectList(new List<object>());

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCategoriesAsync();
            await LoadVehiclesAsync();
            return Page();
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
                var result = await _vehicleServices.GetAllVehiclesAsync();
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Helpper;
using Services.Interfaces;
using Services.DataTransferObject.InventoryDTO;
using Services.DataTransferObject.VehicleCategoryDTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using ElectricVehicleDealerManagermentSystem.SignalR;

namespace ElectricVehicleDealerManagermentSystem.Pages.Inventory
{
    public class InventoryModel : BasePageModel
    {
        private readonly IDealerServices _dealerServices;
        private readonly IVehicleCategoryServices _categoryServices;
        private readonly IHubContext<SignalRHub> _hubContext;

        public InventoryModel(IUserServices userServices, IDealerServices dealerServices, 
            IVehicleCategoryServices categoryServices, IHubContext<SignalRHub> hubContext)
            : base(userServices)
        {
            _dealerServices = dealerServices;
            _categoryServices = categoryServices;
            _hubContext = hubContext;
        }

        public IList<InventoryResponse> InventoryItems { get; set; } = new List<InventoryResponse>();
        public IList<VehicleCategoryResponse> Categories { get; set; } = new List<VehicleCategoryResponse>();
        
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;
        public int? DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public bool ShowOutOfStock { get; set; } = true;

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

        public async Task<IActionResult> OnPostRemoveFromInventoryAsync(int vehicleId)
        {
            // Get dealer information from session
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                TempData["ErrorMessage"] = "Dealer information not found. Please log in again.";
                return RedirectToPage("/Credential/Login");
            }

            try
            {
                var result = await _dealerServices.RemoveFromInventoryAsync(vehicleId, dealerId.Value);
                
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
                TempData["ErrorMessage"] = "An error occurred while removing from inventory: " + ex.Message;
            }

            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            await LoadCategoriesAsync();
            await LoadInventoryAsync();
        }

        private async Task LoadInventoryAsync()
        {
            if (!DealerId.HasValue) return;

            try
            {
                var result = await _dealerServices.GetDealerInventoryAsync(DealerId.Value);
                if (result.Success && result.Data != null)
                {
                    var inventory = result.Data.ToList();

                    // Apply filters
                    if (CategoryFilter.HasValue)
                    {
                        inventory = inventory.Where(i => i.CategoryId == CategoryFilter.Value).ToList();
                    }

                    if (!string.IsNullOrEmpty(SearchTerm))
                    {
                        inventory = inventory.Where(i => 
                            i.VehicleModel.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                            i.VehicleColor.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(i.VehicleVersion) && i.VehicleVersion.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                            i.CategoryName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                        ).ToList();
                    }

                    if (!ShowOutOfStock)
                    {
                        inventory = inventory.Where(i => i.Quantity > 0).ToList();
                    }

                    InventoryItems = inventory;
                }
                else
                {
                    ErrorMessage = result.Message;
                    InventoryItems = new List<InventoryResponse>();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while loading inventory: " + ex.Message;
                InventoryItems = new List<InventoryResponse>();
            }
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
                Categories = new List<VehicleCategoryResponse>();
                CategorySelectList = new SelectList(new List<SelectListItem>());
            }
        }
    }
}

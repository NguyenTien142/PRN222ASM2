using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Helpper;
using Services.Interfaces;
using Services.DataTransferObject.InventoryDTO;
using Microsoft.AspNetCore.SignalR;
using ElectricVehicleDealerManagermentSystem.SignalR;

namespace ElectricVehicleDealerManagermentSystem.Pages.Inventory
{
    public class UpdateModel : BasePageModel
    {
        private readonly IDealerServices _dealerServices;
        private readonly IHubContext<SignalRHub> _hubContext;

        public UpdateModel(IUserServices userServices, IDealerServices dealerServices, IHubContext<SignalRHub> hubContext)
            : base(userServices)
        {
            _dealerServices = dealerServices;
            _hubContext = hubContext;
        }

        [BindProperty]
        public UpdateInventoryRequest InventoryInput { get; set; } = new();

        public InventoryResponse? OriginalInventory { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int? DealerId { get; set; }
        public string DealerName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int? vehicleId)
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

            if (!vehicleId.HasValue)
            {
                TempData["ErrorMessage"] = "Vehicle ID is required.";
                return RedirectToPage("./Index");
            }

            try
            {
                var result = await _dealerServices.GetInventoryItemAsync(vehicleId.Value, DealerId.Value);
                
                if (result.Success && result.Data != null)
                {
                    OriginalInventory = result.Data;
                    InventoryInput = new UpdateInventoryRequest
                    {
                        VehicleId = result.Data.VehicleId,
                        DealerId = result.Data.DealerId,
                        Quantity = result.Data.Quantity
                    };
                    return Page();
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message ?? "Inventory item not found.";
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the inventory item: " + ex.Message;
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Get dealer information
            DealerId = HttpContext.Session.GetInt32("DealerId");
            DealerName = HttpContext.Session.GetString("DealerName") ?? "Unknown Dealer";

            if (!DealerId.HasValue)
            {
                TempData["ErrorMessage"] = "Dealer information not found. Please log in again.";
                return RedirectToPage("/Credential/Login");
            }

            InventoryInput.DealerId = DealerId.Value;

            if (!ModelState.IsValid)
            {
                await LoadOriginalInventoryAsync();
                return Page();
            }

            try
            {
                var result = await _dealerServices.UpdateInventoryAsync(InventoryInput);
                
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
                    await LoadOriginalInventoryAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while updating inventory: " + ex.Message;
                await LoadOriginalInventoryAsync();
                return Page();
            }
        }

        private async Task LoadOriginalInventoryAsync()
        {
            try
            {
                if (DealerId.HasValue && InventoryInput.VehicleId > 0)
                {
                    var result = await _dealerServices.GetInventoryItemAsync(InventoryInput.VehicleId, DealerId.Value);
                    if (result.Success && result.Data != null)
                    {
                        OriginalInventory = result.Data;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the page load
                System.Diagnostics.Debug.WriteLine($"Error loading original inventory: {ex.Message}");
            }
        }
    }
}

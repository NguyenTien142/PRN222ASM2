using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Services.DataTransferObject.VehicleCategoryDTO;
using Microsoft.AspNetCore.SignalR;
using ElectricVehicleDealerManagermentSystem.SignalR;
using ElectricVehicleDealerManagermentSystem.Helpper;

namespace ElectricVehicleDealerManagermentSystem.Pages.Category
{
    public class UpdateModel : BasePageModel
    {
        private readonly IVehicleCategoryServices _categoryServices;
        private readonly IHubContext<SignalRHub> _hubContext;

        public UpdateModel(IUserServices userServices, IVehicleCategoryServices categoryServices, IHubContext<SignalRHub> hubContext)
            : base(userServices)
        {
            _categoryServices = categoryServices;
            _hubContext = hubContext;
        }

        [BindProperty]
        public UpdateVehicleCategoryRequest CategoryInput { get; set; } = new();

        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Credential/Login");
            }

            // Check if user has appropriate role (assuming admin or dealer can manage categories)
            var roleName = HttpContext.Session.GetString("RoleName")?.ToLower();
            if (roleName != "admin" && roleName != "dealer")
            {
                TempData["ErrorMessage"] = "You don't have permission to access this page.";
                return RedirectToPage("/Category/Index");
            }

            if (id == null)
            {
                TempData["ErrorMessage"] = "Category ID is required.";
                return RedirectToPage("./Index");
            }

            try
            {
                var result = await _categoryServices.GetCategoryByIdAsync(id.Value);
                
                if (result.Success && result.Data != null)
                {
                    CategoryInput = new UpdateVehicleCategoryRequest
                    {
                        Id = result.Data.Id,
                        Name = result.Data.Name
                    };
                    return Page();
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the category: " + ex.Message;
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var result = await _categoryServices.UpdateCategoryAsync(CategoryInput);
                
                if (result.Success)
                {
                    // Notify clients
                    await _hubContext.Clients.All.SendAsync("LoadAllItems");

                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToPage("./Index");
                }
                else
                {
                    ErrorMessage = result.Message;
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while updating the category: " + ex.Message;
                return Page();
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;
using Services.DataTransferObject.VehicleCategoryDTO;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.SignalR;
using ElectricVehicleDealerManagermentSystem.SignalR;
using ElectricVehicleDealerManagermentSystem.Helpper;

namespace ElectricVehicleDealerManagermentSystem.Pages.Category
{
    public class CreateModel : BasePageModel
    {
        private readonly IVehicleCategoryServices _categoryServices;
        private readonly IHubContext<SignalRHub> _hubContext;

        public CreateModel(IUserServices userServices, IVehicleCategoryServices categoryServices, IHubContext<SignalRHub> hubContext)
            : base(userServices)
        {
            _categoryServices = categoryServices;
            _hubContext = hubContext;
        }

        [BindProperty]
        public CreateVehicleCategoryRequest CategoryInput { get; set; } = new();

        public string ErrorMessage { get; set; } = string.Empty;

        public IActionResult OnGet()
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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var result = await _categoryServices.CreateCategoryAsync(CategoryInput);
                
                if (result.Success)
                {
                    // Notify clients to reload category list
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
                ErrorMessage = "An error occurred while creating the category: " + ex.Message;
                return Page();
            }
        }
    }
}

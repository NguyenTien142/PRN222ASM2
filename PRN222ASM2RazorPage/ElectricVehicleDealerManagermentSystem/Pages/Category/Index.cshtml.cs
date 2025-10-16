using ElectricVehicleDealerManagermentSystem.Pages.Shared;
using ElectricVehicleDealerManagermentSystem.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Services.DataTransferObject.VehicleCategoryDTO;
using Services.Interfaces;

namespace ElectricVehicleDealerManagermentSystem.Pages.Category
{
    public class IndexModel : BasePageModel
    {
        private readonly IVehicleCategoryServices _categoryServices;
        private readonly IHubContext<SignalRHub> _hubContext;

        public IndexModel(IUserServices userServices, IVehicleCategoryServices categoryServices, IHubContext<SignalRHub> hubContext)
            : base(userServices)
        {
            _categoryServices = categoryServices;
            _hubContext = hubContext;
        }

        public IList<VehicleCategoryResponse> Categories { get; set; } = new List<VehicleCategoryResponse>();
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToPage("/Credential/Login");

            var roleName = HttpContext.Session.GetString("RoleName")?.ToLower();
            if (roleName != "admin" && roleName != "dealer")
            {
                TempData["ErrorMessage"] = "You don't have permission to access this page.";
                return RedirectToPage("/Index");
            }

            await LoadCategoriesAsync();

            if (TempData["SuccessMessage"] != null)
                SuccessMessage = TempData["SuccessMessage"].ToString()!;
            if (TempData["ErrorMessage"] != null)
                ErrorMessage = TempData["ErrorMessage"].ToString()!;

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var result = await _categoryServices.DeleteCategoryAsync(id);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;

                    // Notify all clients to reload
                    await _hubContext.Clients.All.SendAsync("LoadAllItems");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the category: " + ex.Message;
            }

            return RedirectToPage();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var result = await _categoryServices.GetAllCategoriesAsync();
                if (result.Success && result.Data != null)
                {
                    Categories = result.Data.ToList();
                }
                else
                {
                    ErrorMessage = result.Message;
                    Categories = new List<VehicleCategoryResponse>();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while loading categories: " + ex.Message;
                Categories = new List<VehicleCategoryResponse>();
            }
        }
    }
}

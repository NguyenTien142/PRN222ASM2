using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Pages.Shared;
using Services.Interfaces;
using Services.DataTransferObject.VehicleDTO;
using Services.DataTransferObject.VehicleCategoryDTO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.SignalR;
using ElectricVehicleDealerManagermentSystem.SignalR;

namespace ElectricVehicleDealerManagermentSystem.Pages.Vehicle
{
    public class CreateModel : BasePageModel
    {
        private readonly IVehicleServices _vehicleServices;
        private readonly IVehicleCategoryServices _categoryServices;
        private readonly IHubContext<SignalRHub> _hubContext;

        public CreateModel(IUserServices userServices, IHubContext<SignalRHub> hubContext, IVehicleServices vehicleServices, IVehicleCategoryServices categoryServices)
            : base(userServices)
        {
            _vehicleServices = vehicleServices;
            _categoryServices = categoryServices;
            _hubContext = hubContext;
        }

        [BindProperty]
        public CreateVehicleRequest VehicleInput { get; set; } = new();

        [BindProperty]
        public IFormFile? UploadedImage { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
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
                return RedirectToPage("/Vehicle/Index");
            }

            await LoadCategoriesAsync();

            // Set default manufacture date to today
            VehicleInput.ManufactureDate = DateOnly.FromDateTime(DateTime.Today);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            try
            {
                // Validate manufacture date is not in the future
                if (VehicleInput.ManufactureDate > DateOnly.FromDateTime(DateTime.Today))
                {
                    ErrorMessage = "Manufacture date cannot be in the future.";
                    await LoadCategoriesAsync();
                    return Page();
                }

                // Handle image upload
                if (UploadedImage != null)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "vehicles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(UploadedImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await UploadedImage.CopyToAsync(fileStream);
                    }

                    VehicleInput.Image = $"/uploads/vehicles/{uniqueFileName}";
                }

                var result = await _vehicleServices.CreateVehicleAsync(VehicleInput);
                
                if (result.Success)
                {
                    await _hubContext.Clients.All.SendAsync("LoadAllItems");

                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToPage("./Index");
                }
                else
                {
                    ErrorMessage = result.Message;
                    await LoadCategoriesAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while creating the vehicle: " + ex.Message;
                await LoadCategoriesAsync();
                return Page();
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var result = await _categoryServices.GetAllCategoriesAsync();
                if (result.Success && result.Data != null)
                {
                    var categories = result.Data.ToList();
                    CategorySelectList = new SelectList(categories, nameof(VehicleCategoryResponse.Id), nameof(VehicleCategoryResponse.Name), VehicleInput.CategoryId);
                }
                else
                {
                    CategorySelectList = new SelectList(new List<VehicleCategoryResponse>(), nameof(VehicleCategoryResponse.Id), nameof(VehicleCategoryResponse.Name));
                    if (string.IsNullOrEmpty(ErrorMessage))
                        ErrorMessage = "Unable to load categories. " + result.Message;
                }
            }
            catch (Exception ex)
            {
                CategorySelectList = new SelectList(new List<VehicleCategoryResponse>(), nameof(VehicleCategoryResponse.Id), nameof(VehicleCategoryResponse.Name));
                if (string.IsNullOrEmpty(ErrorMessage))
                    ErrorMessage = "An error occurred while loading categories: " + ex.Message;
            }
        }
    }
}

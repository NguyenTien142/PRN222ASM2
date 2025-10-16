using ElectricVehicleDealerManagermentSystem.Pages.Shared;
using ElectricVehicleDealerManagermentSystem.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Services.DataTransferObject.VehicleCategoryDTO;
using Services.DataTransferObject.VehicleDTO;
using Services.Interfaces;
using System.IO;

namespace ElectricVehicleDealerManagermentSystem.Pages.Vehicle
{
    public class UpdateModel : BasePageModel
    {
        private readonly IVehicleServices _vehicleServices;
        private readonly IVehicleCategoryServices _categoryServices;
        private readonly IHubContext<SignalRHub> _hubContext;

        public UpdateModel(IUserServices userServices, IHubContext<SignalRHub> hubContext, IVehicleServices vehicleServices, IVehicleCategoryServices categoryServices)
            : base(userServices)
        {
            _vehicleServices = vehicleServices;
            _categoryServices = categoryServices;
            _hubContext = hubContext;
        }

        [BindProperty]
        public UpdateVehicleRequest VehicleInput { get; set; } = new();

        [BindProperty]
        public IFormFile? UploadedImage { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
        public SelectList CategorySelectList { get; set; } = new SelectList(new List<object>());
        public VehicleResponse? OriginalVehicle { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToPage("/Credential/Login");

            var roleName = HttpContext.Session.GetString("RoleName")?.ToLower();
            if (roleName != "dealer" && roleName != "admin")
            {
                TempData["ErrorMessage"] = "You don't have permission to access this page.";
                return RedirectToPage("/Vehicle/Index");
            }

            if (id == null)
            {
                TempData["ErrorMessage"] = "Vehicle ID is required.";
                return RedirectToPage("./Index");
            }

            try
            {
                await LoadCategoriesAsync();

                var result = await _vehicleServices.GetVehicleByIdAsync(id.Value);

                if (result.Success && result.Data != null)
                {
                    OriginalVehicle = result.Data;
                    VehicleInput = new UpdateVehicleRequest
                    {
                        Id = result.Data.Id,
                        CategoryId = result.Data.CategoryId,
                        Color = result.Data.Color,
                        Price = result.Data.Price,
                        ManufactureDate = result.Data.ManufactureDate,
                        Model = result.Data.Model,
                        Version = result.Data.Version,
                        Image = result.Data.Image
                    };

                    return Page();
                }

                TempData["ErrorMessage"] = result.Message;
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading the vehicle: " + ex.Message;
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                await LoadOriginalVehicleAsync();
                return Page();
            }

            try
            {
                if (VehicleInput.ManufactureDate > DateOnly.FromDateTime(DateTime.Today))
                {
                    ErrorMessage = "Manufacture date cannot be in the future.";
                    await LoadCategoriesAsync();
                    await LoadOriginalVehicleAsync();
                    return Page();
                }

                // Handle image upload if a new one is provided
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

                var result = await _vehicleServices.UpdateVehicleAsync(VehicleInput);

                if (result.Success)
                {
                    await _hubContext.Clients.All.SendAsync("LoadAllItems");

                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToPage("./Index");
                }

                ErrorMessage = result.Message;
                await LoadCategoriesAsync();
                await LoadOriginalVehicleAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while updating the vehicle: " + ex.Message;
                await LoadCategoriesAsync();
                await LoadOriginalVehicleAsync();
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
                    ErrorMessage = "Unable to load categories. " + result.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while loading categories: " + ex.Message;
            }
        }

        private async Task LoadOriginalVehicleAsync()
        {
            try
            {
                var result = await _vehicleServices.GetVehicleByIdAsync(VehicleInput.Id);
                if (result.Success && result.Data != null)
                {
                    OriginalVehicle = result.Data;
                }
            }
            catch { }
        }
    }
}

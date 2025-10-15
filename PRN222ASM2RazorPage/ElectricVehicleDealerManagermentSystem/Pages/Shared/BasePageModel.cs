using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Interfaces;

namespace ElectricVehicleDealerManagermentSystem.Pages.Shared
{
    public class BasePageModel : PageModel
    {
        private readonly IUserServices? _userServices;

        public BasePageModel(IUserServices userServices)
        {
            _userServices = userServices;
        }

        public BasePageModel()
        {
            // Default constructor for pages that don't need UserServices
            _userServices = null;
        }

        public virtual async Task<IActionResult> OnPostLogoutAsync()
        {
            // Get user ID before clearing session
            var userId = HttpContext.Session.GetInt32("UserId");

            // Clear all session data
            HttpContext.Session.Clear();

            // Call logout service if user was logged in and service is available
            if (userId.HasValue && _userServices != null)
            {
                await _userServices.LogoutAsync(userId.Value);
            }

            // Set success message
            TempData["SuccessMessage"] = "You have been successfully logged out.";

            // Redirect to login page
            return RedirectToPage("/Credential/Login");
        }
    }
}
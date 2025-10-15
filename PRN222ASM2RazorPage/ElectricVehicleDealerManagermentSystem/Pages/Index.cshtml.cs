using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Pages.Shared;
using Services.Interfaces;

namespace ElectricVehicleDealerManagermentSystem.Pages
{
    public class IndexModel : BasePageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger, IUserServices userServices) : base(userServices)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                // If not logged in, redirect to login page
                return RedirectToPage("/Credential/Login");
            }

            // If logged in, redirect to appropriate dashboard based on role
            var roleName = HttpContext.Session.GetString("RoleName")?.ToLower();
            
            return roleName switch
            {
                "Customer" => RedirectToPage("/Customer/Index"),
                "Dealer" => RedirectToPage("/Dealer/Index"),
                //"admin" => RedirectToPage("/Admin/Index"),
                _ => Page() // Stay on current page if role is unknown
            };
        }
    }
}

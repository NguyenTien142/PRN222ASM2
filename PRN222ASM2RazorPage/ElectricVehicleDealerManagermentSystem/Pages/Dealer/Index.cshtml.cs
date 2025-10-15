using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Pages.Shared;
using Services.Interfaces;

namespace ElectricVehicleDealerManagermentSystem.Pages.Dealer
{
    public class IndexModel : BasePageModel
    {
        public string? DealerName { get; set; }
        public string? Username { get; set; }
        public int? DealerId { get; set; }

        public IndexModel(IUserServices userServices) : base(userServices)
        {
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Credential/Login");
            }

            // Check if user is a dealer
            var roleName = HttpContext.Session.GetString("RoleName");
            if (roleName?.ToLower() != "dealer")
            {
                return RedirectToPage("/Credential/Login");
            }

            // Get dealer information from session
            DealerName = HttpContext.Session.GetString("DealerName");
            Username = HttpContext.Session.GetString("Username");
            DealerId = HttpContext.Session.GetInt32("DealerId");

            return Page();
        }
    }
}

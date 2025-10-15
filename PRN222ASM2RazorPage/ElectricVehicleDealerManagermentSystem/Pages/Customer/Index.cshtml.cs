using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectricVehicleDealerManagermentSystem.Pages.Shared;
using Services.Interfaces;

namespace ElectricVehicleDealerManagermentSystem.Pages.Customer
{
    public class IndexModel : BasePageModel
    {
        public string? CustomerName { get; set; }
        public string? Username { get; set; }
        public int? CustomerId { get; set; }

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

            // Check if user is a customer
            var roleName = HttpContext.Session.GetString("RoleName");
            if (roleName?.ToLower() != "customer")
            {
                return RedirectToPage("/Credential/Login");
            }

            // Get customer information from session
            CustomerName = HttpContext.Session.GetString("CustomerName");
            Username = HttpContext.Session.GetString("Username");
            CustomerId = HttpContext.Session.GetInt32("CustomerId");

            return Page();
        }
    }
}

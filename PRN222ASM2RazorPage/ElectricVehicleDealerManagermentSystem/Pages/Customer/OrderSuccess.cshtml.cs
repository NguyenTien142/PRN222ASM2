using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectricVehicleDealerManagermentSystem.Pages.Customer
{
    public class OrderSuccessModel : PageModel
    {
        public string? OrderId { get; set; }
        public string? TransactionId { get; set; }
        public string? Amount { get; set; }
        public bool PaymentSuccess { get; set; }

        public IActionResult OnGet()
        {
            // Get data from TempData set by PaymentReturn page
            PaymentSuccess = TempData["PaymentSuccess"] as bool? ?? false;
            OrderId = TempData["OrderId"] as string;
            TransactionId = TempData["TransactionId"] as string;
            Amount = TempData["Amount"] as string;

            // If no payment success data, redirect to orders
            if (!PaymentSuccess)
            {
                return RedirectToPage("/Customer/MyOrders");
            }

            return Page();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectricVehicleDealerManagermentSystem.Pages.Customer
{
    public class PaymentFailedModel : PageModel
    {
        public string FailureReason { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string? TransactionId { get; set; }

        public IActionResult OnGet(string reason = "Unknown error", int orderId = 0, string? transactionId = null)
        {
            FailureReason = reason;
            OrderId = orderId;
            TransactionId = transactionId;

            return Page();
        }
    }
}
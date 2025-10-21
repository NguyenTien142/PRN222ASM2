using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.Helpper.VNPay;
using Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using ElectricVehicleDealerManagermentSystem.SignalR;

namespace ElectricVehicleDealerManagermentSystem.Pages.Customer
{
    public class PaymentReturnModel : PageModel
    {
        private readonly VnPayService _vnPayService;
        private readonly IOrderServices _orderServices;
        private readonly IHubContext<SignalRHub> _hubContext;

        public PaymentReturnModel(VnPayService vnPayService, IOrderServices orderServices, IHubContext<SignalRHub> hubContext)
        {
            _vnPayService = vnPayService;
            _orderServices = orderServices;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // 🧩 This only runs when VNPay redirects the browser back
                if (Request.Query.Count == 0)
                    return RedirectToPage("/Customer/PaymentFailed", new { reason = "No VNPay parameters received" });

                // Parse the VNPay response from query string (browser redirect)
                var response = _vnPayService.PaymentExecute(Request.Query);

                // Simulate extracting Order ID from OrderInfo
                int orderId = ExtractOrderIdFromVNPay(response);

                // For demo: treat any ResponseCode == "00" as success
                bool isPaymentSuccess = response.VnPayResponseCode == "00";

                if (isPaymentSuccess)
                {
                    // ✅ Simulate marking the order as PAID
                    await _orderServices.UpdateOrderStatusWithInventory(orderId, "PAID");

                    // ✅ Notify via SignalR (for real-time refresh)
                    await _hubContext.Clients.All.SendAsync("LoadAllItems");

                    // ✅ Show success page with mock data
                    TempData["PaymentSuccess"] = true;
                    TempData["OrderId"] = orderId.ToString();
                    TempData["TransactionId"] = response.TransactionId ?? "DEMO-TXN";
                    TempData["Amount"] = ExtractAmountFromVNPay();

                    return RedirectToPage("/Customer/OrderSuccess");
                }
                else
                {
                    // ❌ Simulated failure
                    string reason = GetFailureReason(response.VnPayResponseCode);
                    return RedirectToPage("/Customer/PaymentFailed", new { reason, orderId });
                }
            }
            catch (Exception ex)
            {
                return RedirectToPage("/Customer/PaymentFailed", new { reason = "Error: " + ex.Message });
            }
        }

        private int ExtractOrderIdFromVNPay(Services.DataTransferObject.VNPay.VNPaymentResponse response)
        {
            try
            {
                if (!string.IsNullOrEmpty(response.OrderDescription))
                {
                    var parts = response.OrderDescription.Split(' ');
                    if (int.TryParse(parts[0], out int id)) return id;
                }

                if (Request.Query.ContainsKey("vnp_OrderInfo"))
                {
                    var orderInfo = Uri.UnescapeDataString(Request.Query["vnp_OrderInfo"]);
                    var parts = orderInfo.Split(' ');
                    if (int.TryParse(parts[0], out int id)) return id;
                }
            }
            catch { }
            return 0;
        }

        private string ExtractAmountFromVNPay()
        {
            try
            {
                if (Request.Query.ContainsKey("vnp_Amount"))
                {
                    var amountString = Request.Query["vnp_Amount"].ToString();
                    if (long.TryParse(amountString, out long amount))
                    {
                        decimal actualAmount = amount / 100m;
                        return actualAmount.ToString("C0");
                    }
                }
            }
            catch { }
            return "N/A";
        }

        private string GetFailureReason(string code)
        {
            return code switch
            {
                "07" => "Transaction expired",
                "09" => "Customer cancelled transaction",
                "10" => "Incorrect info entered",
                "11" => "Transaction timeout",
                "12" => "Account locked",
                "24" => "Customer cancelled",
                "51" => "Insufficient funds",
                "65" => "Exceeded daily limit",
                "75" => "Bank maintenance",
                _ => $"Payment failed (Code: {code})"
            };
        }
    }
}

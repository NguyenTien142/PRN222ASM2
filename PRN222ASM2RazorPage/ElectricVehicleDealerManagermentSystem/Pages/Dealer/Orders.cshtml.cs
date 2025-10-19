using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.DataTransferObject.OrderDTO;
using Services.DataTransferObject.Common;
using Services.Interfaces;

namespace ElectricVehicleDealerManagermentSystem.Pages.Dealer
{
    public class OrdersModel : PageModel
    {
        private readonly IOrderServices _orderServices;

        public OrdersModel(IOrderServices orderServices)
        {
            _orderServices = orderServices;
        }

        public List<OrderResponse> Orders { get; set; } = new List<OrderResponse>();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? DebugMessage { get; set; }

        [BindProperty]
        public int OrderId { get; set; }

        [BindProperty]
        public string NewStatus { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in as dealer
            var userRole = HttpContext.Session.GetString("RoleName");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "dealer")
            {
                return RedirectToPage("/Credential/Login");
            }

            await LoadOrders();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync()
        {
            // Check if user is logged in as dealer
            var userRole = HttpContext.Session.GetString("RoleName");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "dealer")
            {
                return RedirectToPage("/Credential/Login");
            }

            if (OrderId <= 0 || string.IsNullOrEmpty(NewStatus))
            {
                ErrorMessage = "Invalid order ID or status.";
                await LoadOrders();
                return Page();
            }

            // Get dealer ID from session to validate ownership
            var dealerId = HttpContext.Session.GetInt32("DealerId");
            if (!dealerId.HasValue)
            {
                ErrorMessage = "Dealer information not found.";
                await LoadOrders();
                return Page();
            }

            // Load orders first to check if this order belongs to this dealer
            await LoadOrders();
            var orderToUpdate = Orders.FirstOrDefault(o => o.Id == OrderId);
            if (orderToUpdate == null)
            {
                ErrorMessage = $"Order #{OrderId} not found or does not contain vehicles managed by your dealership.";
                return Page();
            }

            // Use different method based on status
            ServiceResponse result;
            if (NewStatus.ToUpper() == "PAID")
            {
                // Use method that reduces inventory when marking as PAID
                result = await _orderServices.UpdateOrderStatusWithInventory(OrderId, NewStatus);
            }
            else
            {
                // Use regular method for other status changes
                result = await _orderServices.UpdateOrderStatus(OrderId, NewStatus);
            }
            
            if (result.Success)
            {
                SuccessMessage = result.Message;
            }
            else
            {
                ErrorMessage = result.Message;
            }

            // Reload orders to show updated status
            await LoadOrders();
            return Page();
        }

        private async Task LoadOrders()
        {
            try
            {
                // Get dealer ID from session
                var dealerId = HttpContext.Session.GetInt32("DealerId");
                if (!dealerId.HasValue)
                {
                    ErrorMessage = "Dealer information not found.";
                    DebugMessage = "DealerId not found in session";
                    return;
                }

                // Get orders for this specific dealer only
                var result = await _orderServices.GetOrdersByDealer(dealerId.Value);
                
                if (result.Success && result.Data != null)
                {
                    Orders = result.Data;
                    if (!Orders.Any())
                    {
                        DebugMessage = result.Message; // This will show if no orders found for this dealer
                    }
                }
                else
                {
                    ErrorMessage = result.Message;
                    Orders = new List<OrderResponse>();
                    DebugMessage = $"Service call failed: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while loading orders.";
                DebugMessage = $"Exception: {ex.Message}";
                Orders = new List<OrderResponse>();
            }
        }
    }
}
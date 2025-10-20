using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.DataTransferObject.OrderDTO;
using Services.Interfaces;

namespace ElectricVehicleDealerManagermentSystem.Pages.Customer
{
    public class MyOrdersModel : PageModel
    {
        private readonly IOrderServices _orderServices;

        public MyOrdersModel(IOrderServices orderServices)
        {
            _orderServices = orderServices;
        }

        public List<OrderResponse> MyOrders { get; set; } = new List<OrderResponse>();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? DebugMessage { get; set; }

        [BindProperty]
        public int OrderId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in as customer
            var userRole = HttpContext.Session.GetString("RoleName");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "customer")
            {
                return RedirectToPage("/Credential/Login");
            }

            await LoadMyOrders();
            return Page();
        }

        public async Task<IActionResult> OnPostMarkAsDoneAsync()
        {
            try
            {
                // Check if user is logged in as customer
                var userRole = HttpContext.Session.GetString("RoleName");
                if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "customer")
                {
                    return RedirectToPage("/Credential/Login");
                }

                if (OrderId <= 0)
                {
                    ErrorMessage = "Invalid order ID.";
                    await LoadMyOrders();
                    return Page();
                }

                // Get customer ID
                var customerId = HttpContext.Session.GetInt32("CustomerId");
                if (!customerId.HasValue)
                {
                    ErrorMessage = "Customer information not found. Please login again.";
                    return RedirectToPage("/Credential/Login");
                }

                // Load orders first to verify ownership
                await LoadMyOrders();

                // Find the specific order
                var orderToUpdate = MyOrders.FirstOrDefault(o => o.Id == OrderId);
                if (orderToUpdate == null)
                {
                    ErrorMessage = $"Order #{OrderId} not found.";
                    DebugMessage = $"Available orders: {string.Join(", ", MyOrders.Select(o => o.Id))}. Customer ID: {customerId}";
                    await LoadMyOrders();
                    return Page();
                }

                // Verify ownership
                if (orderToUpdate.CustomerId != customerId.Value)
                {
                    ErrorMessage = $"Order #{OrderId} does not belong to you.";
                    DebugMessage = $"Order Customer ID: {orderToUpdate.CustomerId}, Your Customer ID: {customerId}";
                    await LoadMyOrders();
                    return Page();
                }

                // Check if status is DELIVERING (changed from PAID)
                if (orderToUpdate.Status != "DELIVERING")
                {
                    ErrorMessage = $"Only orders that are being delivered can be marked as done. Current status: {orderToUpdate.Status}";
                    await LoadMyOrders();
                    return Page();
                }

                // Update order status
                var result = await _orderServices.UpdateOrderStatus(OrderId, "DONE");
                
                if (result.Success)
                {
                    SuccessMessage = "Order marked as completed successfully! Thank you for confirming receipt.";
                }
                else
                {
                    ErrorMessage = $"Failed to update order: {result.Message}";
                }

                // Reload orders to show updated status
                await LoadMyOrders();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while updating the order.";
                DebugMessage = $"Exception: {ex.Message}";
                await LoadMyOrders();
                return Page();
            }
        }

        private async Task LoadMyOrders()
        {
            try
            {
                var customerId = HttpContext.Session.GetInt32("CustomerId");
                if (!customerId.HasValue)
                {
                    ErrorMessage = "Customer information not found.";
                    DebugMessage = "CustomerId not found in session";
                    return;
                }

                var result = await _orderServices.GetAllOrders();
                
                if (result.Success && result.Data != null)
                {
                    // Filter orders for this specific customer
                    MyOrders = result.Data.Where(o => o.CustomerId == customerId.Value)
                                         .OrderByDescending(o => o.OrderDate)
                                         .ToList();
                    
                    if (!MyOrders.Any())
                    {
                        DebugMessage = $"No orders found for Customer ID: {customerId}. Total orders in system: {result.Data.Count}";
                    }
                    else
                    {
                        DebugMessage = $"Found {MyOrders.Count} orders for Customer ID: {customerId}";
                    }
                }
                else
                {
                    ErrorMessage = result.Message;
                    MyOrders = new List<OrderResponse>();
                    DebugMessage = $"Service call failed: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while loading your orders.";
                DebugMessage = $"Exception in LoadMyOrders: {ex.Message}";
                MyOrders = new List<OrderResponse>();
            }
        }
    }
}
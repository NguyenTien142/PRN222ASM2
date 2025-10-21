using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.DataTransferObject.OrderDTO;
using Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using ElectricVehicleDealerManagermentSystem.SignalR;
using Services.Helpper.VNPay;
using Services.DataTransferObject.VNPay;

namespace ElectricVehicleDealerManagermentSystem.Pages.Customer
{
    public class MyOrdersModel : PageModel
    {
        private readonly IOrderServices _orderServices;
        private readonly IHubContext<SignalRHub> _hubContext;
        private readonly VnPayService _vnPayService;

        public MyOrdersModel(IOrderServices orderServices, IHubContext<SignalRHub> hubContext, VnPayService vnPayService)
        {
            _orderServices = orderServices;
            _hubContext = hubContext;
            _vnPayService = vnPayService;
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

        public async Task<IActionResult> OnPostPayAsync(int orderId)
        {
            try
            {
                // Check if user is logged in as customer
                var userRole = HttpContext.Session.GetString("RoleName");
                if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "customer")
                {
                    return RedirectToPage("/Credential/Login");
                }

                if (orderId <= 0)
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
                var orderToPay = MyOrders.FirstOrDefault(o => o.Id == orderId);
                if (orderToPay == null)
                {
                    ErrorMessage = $"Order #{orderId} not found.";
                    await LoadMyOrders();
                    return Page();
                }

                // Verify ownership
                if (orderToPay.CustomerId != customerId.Value)
                {
                    ErrorMessage = $"Order #{orderId} does not belong to you.";
                    await LoadMyOrders();
                    return Page();
                }

                // Check if status is PENDING
                if (orderToPay.Status != "PENDING")
                {
                    ErrorMessage = $"Only pending orders can be paid. Current status: {orderToPay.Status}";
                    await LoadMyOrders();
                    return Page();
                }

                // Create payment information for VNPay
                var paymentInfo = new PaymentInformationModel
                {
                    OrderId = orderId,
                    Amount = orderToPay.TotalAmount,
                    OrderDescription = $"Payment for Vehicle Order #{orderId}",
                    OrderType = "vehicle_purchase"
                };

                // Generate VNPay payment URL
                var paymentUrl = _vnPayService.CreatePaymentUrl(paymentInfo, HttpContext);

                // Redirect to VNPay payment page
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while processing payment: " + ex.Message;
                await LoadMyOrders();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostVerifyPaymentAsync(int orderId, string transactionId)
        {
            try
            {
                // Check if user is logged in as customer
                var userRole = HttpContext.Session.GetString("RoleName");
                if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "customer")
                {
                    return RedirectToPage("/Credential/Login");
                }

                if (orderId <= 0)
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
                var orderToVerify = MyOrders.FirstOrDefault(o => o.Id == orderId);
                if (orderToVerify == null)
                {
                    ErrorMessage = $"Order #{orderId} not found.";
                    await LoadMyOrders();
                    return Page();
                }

                // Verify ownership
                if (orderToVerify.CustomerId != customerId.Value)
                {
                    ErrorMessage = $"Order #{orderId} does not belong to you.";
                    await LoadMyOrders();
                    return Page();
                }

                // Check if status is PENDING
                if (orderToVerify.Status != "PENDING")
                {
                    ErrorMessage = $"This order has already been processed. Current status: {orderToVerify.Status}";
                    await LoadMyOrders();
                    return Page();
                }

                // Update order status to PAID (since VNPay payment was successful)
                var result = await _orderServices.UpdateOrderStatusWithInventory(orderId, "PAID");
                
                if (result.Success)
                {
                    // Send real-time notification to refresh order pages
                    await _hubContext.Clients.All.SendAsync("LoadAllItems");
                    
                    SuccessMessage = $"Payment verified successfully! Order #{orderId} has been marked as PAID. Transaction ID: {transactionId}";
                }
                else
                {
                    ErrorMessage = $"Failed to update order status: {result.Message}";
                }

                // Reload orders to show updated status
                await LoadMyOrders();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while verifying payment: " + ex.Message;
                await LoadMyOrders();
                return Page();
            }
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
                    // Send real-time notification to refresh order pages
                    await _hubContext.Clients.All.SendAsync("LoadAllItems");
                    
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
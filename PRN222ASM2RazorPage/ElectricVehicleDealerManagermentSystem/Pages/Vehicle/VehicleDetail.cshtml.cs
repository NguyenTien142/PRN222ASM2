using ElectricVehicleDealerManagermentSystem.Helpper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Repositories.Context;
using Repositories.Model;
using Services.DataTransferObject.AppointmentDTO;
using Services.DataTransferObject.VehicleDTO;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElectricVehicleDealerManagermentSystem.Pages.Vehicle
{
    public class VehicleDetailModel : BasePageModel
    {
        private readonly IVehicleServices vehicleServices;
        private readonly IUserServices userService;
        private readonly IOrderServices orderServices;
        private readonly IAppointmentServices _appointmentServices;


        public VehicleDetailModel(
            IVehicleServices _vehicleServices, 
            IUserServices _userService, 
            IOrderServices _orderServices,
            IAppointmentServices appointmentServices)
        {
            vehicleServices = _vehicleServices;
            userService = _userService;
            orderServices = _orderServices;
            _appointmentServices = appointmentServices;
        }

        // Properties to bind to the view
        public VehicleResponse? Vehicle { get; set; }
        public bool IsUserLoggedIn { get; private set; }
        public string? UserRole { get; private set; }
        public int? CurrentUserId { get; private set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, int? userId = null)
        {
            // Set user information
            CurrentUserId = HttpContext.Session.GetInt32("UserId");
            UserRole = HttpContext.Session.GetString("RoleName")?.ToLower();
            IsUserLoggedIn = CurrentUserId.HasValue;

            if (userId.HasValue)
            {
                ViewData["UserId"] = userId.Value;
            }

            // Load vehicle details
            await LoadVehicleDetailAsync(id);
            
            // Check if vehicle was found
            if (Vehicle == null)
            {
                return NotFound("Vehicle not found.");
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostBuyAsync(int vehicleId)
        {
            try
            {
                // Check if user is logged in
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (!currentUserId.HasValue)
                {
                    TempData["ErrorMessage"] = "Please login to purchase a vehicle.";
                    return RedirectToPage("/Credential/Login");
                }

                // Check if user is customer
                var userRole = HttpContext.Session.GetString("RoleName")?.ToLower();
                if (userRole != "customer")
                {
                    TempData["ErrorMessage"] = "Only customers can purchase vehicles.";
                    return RedirectToPage(new { id = vehicleId });
                }

                // Get customer ID from session
                var customerId = HttpContext.Session.GetInt32("CustomerId");
                if (!customerId.HasValue)
                {
                    TempData["ErrorMessage"] = "Customer information not found. Please login again.";
                    return RedirectToPage("/Credential/Login");

                }

                // Get vehicle to retrieve price
                var vehicleResult = await vehicleServices.GetVehicleByIdAsync(vehicleId);
                if (!vehicleResult.Success || vehicleResult.Data == null)
                {
                    TempData["ErrorMessage"] = vehicleResult.Message ?? "Vehicle not found.";
                    return RedirectToPage(new { id = vehicleId });
                }

                // Create order
                var result = await orderServices.CreateOrder(customerId.Value, vehicleId, vehicleResult.Data.Price);
                
                if (result.Success)
                {
                    SuccessMessage = "Order created successfully!";
                    await LoadVehicleDetailAsync(vehicleId); // ? Reload vehicle data
                    return Page();
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message ?? "Failed to create order.";
                    return RedirectToPage(new { id = vehicleId });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while processing your order. Please try again.";
                // In production, log the exception properly
                // Logger.LogError(ex, "Error processing order for vehicle: {VehicleId}", vehicleId);
                return RedirectToPage(new { id = vehicleId });
            }
        }

        private async Task LoadVehicleDetailAsync(int id)
        {
            try
            {
                var result = await vehicleServices.GetVehicleByIdAsync(id);
                if (result.Success && result.Data != null)
                {
                    Vehicle = result.Data;
                }
                else
                {
                    ErrorMessage = result.Message ?? "Vehicle not found.";
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while loading vehicle details. Please try again.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                // In production, log the exception properly
                // Logger.LogError(ex, "Error loading vehicle details for ID: {VehicleId}", id);
            }
        }

        public async Task<IActionResult> OnPostCreateAppointmentAsync(int vehicleId, DateTime appointmentDate)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (!currentUserId.HasValue)
                {
                    TempData["ErrorMessage"] = "Please login to schedule an appointment.";
                    return RedirectToPage("/Credential/Login");
                }

                var userRole = HttpContext.Session.GetString("RoleName")?.ToLower();
                if (userRole != "customer")
                {
                    TempData["ErrorMessage"] = "Only customers can schedule appointments.";
                    return RedirectToPage(new { id = vehicleId });
                }

                var customerId = HttpContext.Session.GetInt32("CustomerId");
                if (!customerId.HasValue)
                {
                    TempData["ErrorMessage"] = "Customer information not found. Please login again.";
                    return RedirectToPage("/Credential/Login");
                }

                if (appointmentDate <= DateTime.Now)
                {
                    TempData["ErrorMessage"] = "Appointment date must be in the future.";
                    return RedirectToPage(new { id = vehicleId });
                }

                var request = new CreateAppointmentRequest
                {
                    CustomerId = customerId.Value,
                    VehicleId = vehicleId,
                    AppointmentDate = appointmentDate,
                    Status = "Pending"
                };

                var result = await _appointmentServices.CreateAppointmentAsync(request);
                
                if (result.Success)
                {
                    SuccessMessage = "Appointment scheduled successfully!";
                    await LoadVehicleDetailAsync(vehicleId);
                    return Page();
                }
                else
                {
                    ErrorMessage = result.Message ?? "Failed to schedule appointment.";
                    await LoadVehicleDetailAsync(vehicleId);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while scheduling your appointment. Please try again.";
                await LoadVehicleDetailAsync(vehicleId);
                return Page();
            }
        }
    }
}

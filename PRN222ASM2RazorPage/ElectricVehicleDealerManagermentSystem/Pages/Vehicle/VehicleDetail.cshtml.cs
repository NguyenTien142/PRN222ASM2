using ElectricVehicleDealerManagermentSystem.Helpper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Repositories.Context;
using Repositories.Model;
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

        public VehicleDetailModel(IVehicleServices _vehicleServices, IUserServices _userService)
        {
           vehicleServices = _vehicleServices;
           userService = _userService;
        }

        // Properties to bind to the view
        public VehicleResponse? Vehicle { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsUserLoggedIn { get; private set; }
        public string? UserRole { get; private set; }
        public int? CurrentUserId { get; private set; }

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
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while loading vehicle details. Please try again.";
                // In production, log the exception properly
                // Logger.LogError(ex, "Error loading vehicle details for ID: {VehicleId}", id);
            }
        }

        // Method to check if current user can edit this vehicle
        public bool CanEditVehicle()
        {
            return IsUserLoggedIn && (UserRole == "admin" || UserRole == "dealer");
        }

        // Method to check if current user can delete this vehicle
        public bool CanDeleteVehicle()
        {
            return IsUserLoggedIn && (UserRole == "admin" || UserRole == "dealer");
        }

        // Method to get display name for current user
        public string GetUserDisplayName()
        {
            if (!IsUserLoggedIn) return "Guest";
            
            return HttpContext.Session.GetString("CustomerName") ?? 
                   HttpContext.Session.GetString("DealerName") ?? 
                   HttpContext.Session.GetString("Username") ?? 
                   "User";
        }
    }
}

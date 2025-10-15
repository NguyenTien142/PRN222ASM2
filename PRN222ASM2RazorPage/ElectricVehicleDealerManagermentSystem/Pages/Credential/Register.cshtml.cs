using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Services.DataTransferObject.UserDTO;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace ElectricVehicleDealerManagermentSystem.Pages.Credential
{
    public class RegisterModel : PageModel
    {
        private readonly IUserServices _userServices;

        public RegisterModel(IUserServices userServices)
        {
            _userServices = userServices;
        }

        [BindProperty]
        public RegisterInputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        // Role options for dropdown
        public List<SelectListItem> RoleOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "Customer" },
            new SelectListItem { Value = "2", Text = "Dealer" }
        };

        public class RegisterInputModel
        {
            [Required(ErrorMessage = "Username is required")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
            [Display(Name = "Username")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
            [Display(Name = "Password")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your password")]
            [Display(Name = "Confirm Password")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email address is required")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address")]
            [Display(Name = "Email Address")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please select a role")]
            [Display(Name = "Register as")]
            public int RoleId { get; set; }

            // Customer specific fields
            [Display(Name = "Full Name")]
            public string? CustomerName { get; set; }

            [Display(Name = "Phone Number")]
            [Phone(ErrorMessage = "Please enter a valid phone number")]
            public string? CustomerPhone { get; set; }

            [Display(Name = "Address")]
            public string? CustomerAddress { get; set; }

            // Dealer specific fields
            [Display(Name = "Dealer Name")]
            public string? DealerName { get; set; }

            [Display(Name = "Dealer Address")]
            public string? DealerAddress { get; set; }

            [Display(Name = "Initial Vehicle Quantity")]
            [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a positive number")]
            public int? DealerQuantity { get; set; }
        }

        public void OnGet()
        {
            // Check if user is already logged in
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                Response.Redirect("/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Custom validation based on role
            ValidateRoleSpecificFields();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var registerRequest = new RegisterRequest
                {
                    Username = Input.Username,
                    Password = Input.Password,
                    Email = Input.Email,  // Now at user level
                    RoleId = Input.RoleId,
                    
                    // Customer fields (email removed)
                    CustomerName = Input.CustomerName,
                    CustomerPhone = Input.CustomerPhone,
                    CustomerAddress = Input.CustomerAddress,
                    
                    // Dealer fields
                    DealerName = Input.DealerName,
                    DealerAddress = Input.DealerAddress,
                    DealerQuantity = Input.DealerQuantity ?? 0
                };

                var result = await _userServices.RegisterAsync(registerRequest);

                if (result.Success)
                {
                    SuccessMessage = result.Message + " You can now login with your credentials.";
                    
                    // Clear the form
                    Input = new RegisterInputModel();
                    
                    // Optionally redirect to login page after a delay
                    TempData["SuccessMessage"] = "Registration successful! Please login with your credentials.";
                    return RedirectToPage("./Login");
                }
                else
                {
                    ErrorMessage = result.Message;
                    ModelState.AddModelError(string.Empty, result.Message);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred during registration. Please try again.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                // Log the exception in production
            }

            return Page();
        }

        private void ValidateRoleSpecificFields()
        {
            if (Input.RoleId == 1) // Customer
            {
                if (string.IsNullOrWhiteSpace(Input.CustomerName))
                    ModelState.AddModelError("Input.CustomerName", "Full Name is required for customer registration");
                
                if (string.IsNullOrWhiteSpace(Input.CustomerPhone))
                    ModelState.AddModelError("Input.CustomerPhone", "Phone Number is required for customer registration");
                
                if (string.IsNullOrWhiteSpace(Input.CustomerAddress))
                    ModelState.AddModelError("Input.CustomerAddress", "Address is required for customer registration");
            }
            else if (Input.RoleId == 2) // Dealer
            {
                if (string.IsNullOrWhiteSpace(Input.DealerName))
                    ModelState.AddModelError("Input.DealerName", "Dealer Name is required for dealer registration");
                
                if (string.IsNullOrWhiteSpace(Input.DealerAddress))
                    ModelState.AddModelError("Input.DealerAddress", "Dealer Address is required for dealer registration");
                
                if (!Input.DealerQuantity.HasValue)
                    ModelState.AddModelError("Input.DealerQuantity", "Initial Vehicle Quantity is required for dealer registration");
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.DataTransferObject.UserDTO;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace ElectricVehicleDealerManagermentSystem.Pages.Credential
{
    public class LoginModel : PageModel
    {
        private readonly IUserServices _userServices;

        public LoginModel(IUserServices userServices)
        {
            _userServices = userServices;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public class LoginInputModel
        {
            [Required(ErrorMessage = "Username is required")]
            [Display(Name = "Username")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [Display(Name = "Password")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var loginRequest = new LoginRequest
                {
                    Username = Input.Username,
                    Password = Input.Password
                };

                var result = await _userServices.LoginAsync(loginRequest);

                if (result.Success && result.User != null)
                {
                    // Store user session data
                    HttpContext.Session.SetInt32("UserId", result.User.Id);
                    HttpContext.Session.SetString("Username", result.User.Username);
                    HttpContext.Session.SetInt32("RoleId", result.User.RoleId);
                    HttpContext.Session.SetString("RoleName", result.User.RoleName);

                    // Store additional user type information
                    if (result.User.Customer != null)
                    {
                        HttpContext.Session.SetInt32("CustomerId", result.User.Customer.Id);
                        HttpContext.Session.SetString("CustomerName", result.User.Customer.Name);
                    }
                    else if (result.User.Dealer != null)
                    {
                        HttpContext.Session.SetInt32("DealerId", result.User.Dealer.Id);
                        HttpContext.Session.SetString("DealerName", result.User.Dealer.DealerName);
                    }

                    SuccessMessage = result.Message;

                    // Redirect based on role
                    if (result.User.RoleName.ToLower() == "admin")
                    {
                        return RedirectToPage("/Admin/Dashboard");
                    }
                    else if (result.User.RoleName.ToLower() == "dealer")
                    {
                        return RedirectToPage("/Dealer/Dashboard");
                    }
                    else if (result.User.RoleName.ToLower() == "customer")
                    {
                        return RedirectToPage("/Customer/Dashboard");
                    }
                    else
                    {
                        return RedirectToPage("/Index");
                    }
                }
                else
                {
                    ErrorMessage = result.Message;
                    ModelState.AddModelError(string.Empty, result.Message);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred during login. Please try again.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                // Log the exception in production
            }

            return Page();
        }
    }
}

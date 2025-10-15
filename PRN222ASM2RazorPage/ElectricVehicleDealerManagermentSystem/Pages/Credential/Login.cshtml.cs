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
            [Required(ErrorMessage = "Username or Email is required")]
            [Display(Name = "Username or Email")]
            public string UsernameOrEmail { get; set; } = string.Empty;

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
                var roleName = HttpContext.Session.GetString("RoleName")?.ToLower();
                if (roleName == "customer")
                {
                    Response.Redirect("/Customer/Index");
                }
                else if (roleName == "dealer")
                {
                    Response.Redirect("/Dealer/Index");
                }
                else if (roleName == "admin")
                {
                    Response.Redirect("/Admin/Index");
                }
                else
                {
                    Response.Redirect("/Index");
                }
            }

            // Handle success message from TempData (like after logout or registration)
            if (TempData.ContainsKey("SuccessMessage"))
            {
                SuccessMessage = TempData["SuccessMessage"]?.ToString();
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
                // Since we now accept username or email, we'll pass the input as username
                // The UserServices will need to be updated to handle both
                var loginRequest = new LoginRequest
                {
                    Username = Input.UsernameOrEmail,
                    Password = Input.Password
                };

                var result = await _userServices.LoginAsync(loginRequest);

                if (result.Success && result.User != null)
                {
                    // Store comprehensive user session data
                    HttpContext.Session.SetInt32("UserId", result.User.Id);
                    HttpContext.Session.SetString("Username", result.User.Username);
                    HttpContext.Session.SetString("Email", result.User.Email);
                    HttpContext.Session.SetInt32("RoleId", result.User.RoleId);
                    HttpContext.Session.SetString("RoleName", result.User.RoleName);

                    // Store additional user type information
                    if (result.User.Customer != null)
                    {
                        HttpContext.Session.SetInt32("CustomerId", result.User.Customer.Id);
                        HttpContext.Session.SetString("CustomerName", result.User.Customer.Name);
                        HttpContext.Session.SetString("CustomerPhone", result.User.Customer.Phone);
                        HttpContext.Session.SetString("CustomerAddress", result.User.Customer.Address);
                    }
                    else if (result.User.Dealer != null)
                    {
                        HttpContext.Session.SetInt32("DealerId", result.User.Dealer.Id);
                        HttpContext.Session.SetString("DealerName", result.User.Dealer.DealerName);
                        HttpContext.Session.SetString("DealerAddress", result.User.Dealer.Address);
                        HttpContext.Session.SetInt32("DealerQuantity", result.User.Dealer.Quantity);
                    }

                    // Set login success message
                    TempData["LoginSuccessMessage"] = $"Welcome back, {GetDisplayName(result.User)}!";

                    // Redirect based on role to Index pages
                    return result.User.RoleName.ToLower() switch
                    {
                        "admin" => RedirectToPage("/Admin/Index"),
                        "dealer" => RedirectToPage("/Dealer/Index"),
                        "customer" => RedirectToPage("/Customer/Index"),
                        _ => RedirectToPage("/Index")
                    };
                }
                else
                {
                    ErrorMessage = result.Message ?? "Invalid login credentials.";
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred during login. Please try again.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                // In production, log the exception properly
                // Logger.LogError(ex, "Login error for user: {Username}", Input.UsernameOrEmail);
            }

            return Page();
        }

        private static string GetDisplayName(GetUserRespond user)
        {
            return user.Customer?.Name ?? user.Dealer?.DealerName ?? user.Username;
        }
    }
}

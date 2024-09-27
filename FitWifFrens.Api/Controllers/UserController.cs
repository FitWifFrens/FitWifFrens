using FitWifFrens.Api.Dtos.User;
using FitWifFrens.Api.Services;
using FitWifFrens.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Web;

namespace FitWifFrens.Api.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly JwtTokenService _tokenService;
        private readonly IEmailSender _emailSender;

        public UserController(UserManager<User> userManager, SignInManager<User> signInManager,
            JwtTokenService tokenService, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailSender = emailSender;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(user => user.Email == loginDto.Email.ToLower());

            if (user == null)
            {
                return Unauthorized("Invalid username");
            }

            var loginResult = await _signInManager.PasswordSignInAsync(user, loginDto.Password, loginDto.RememberMe, false);

            if (!loginResult.Succeeded)
            {
                return Unauthorized("Username not found and/or password incorrect");
            }

            // Sign in the user with cookies
            var claims = new List<Claim>
            {
                new (ClaimTypes.Name, user.UserName),
                new (ClaimTypes.Email, user.Email),
                new (ClaimTypes.NameIdentifier, user.Id)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return Ok(
                new NewUserDto
                {
                    UserName = user.Email,
                    Token = _tokenService.GenerateToken(user)
                });
        }

        [HttpGet("external-login/{provider}")]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUri = Url.Action("ExternalLoginCallback", "User", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUri);
            return Challenge(properties, provider);
        }

        [HttpGet("external-login-callback")]
        public async Task<IActionResult> ExternalLoginCallback()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return BadRequest("Error loading external login information.");
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(info.Principal.FindFirstValue(ClaimTypes.Email));
                if (user != null)
                {
                    // Sign in the user with cookies
                    var claims = new List<Claim>
                    {
                        new (ClaimTypes.Name, user.UserName),
                        new (ClaimTypes.Email, user.Email),
                        new (ClaimTypes.NameIdentifier, user.Id)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return Ok(new
                    {
                        Token = _tokenService.GenerateToken(user),
                        UserName = user.Email
                    });
                }
            }

            // If the user does not have an account, create one
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email != null)
            {
                var user = new User
                {
                    UserName = email,
                    Email = email
                };

                var identityResult = await _userManager.CreateAsync(user);
                if (identityResult.Succeeded)
                {
                    await _userManager.AddLoginAsync(user, info);
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Sign in the user with cookies
                    var claims = new List<Claim>
                    {
                        new (ClaimTypes.Name, user.UserName),
                        new (ClaimTypes.Email, user.Email),
                        new (ClaimTypes.NameIdentifier, user.Id)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return Ok(new
                    {
                        Token = _tokenService.GenerateToken(user),
                        UserName = user.Email
                    });
                }
            }

            return BadRequest("Failed to login via external provider.");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = new User
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email
                };

                var createdUser = await _userManager.CreateAsync(user, registerDto.Password);

                if (!createdUser.Succeeded)
                {
                    return StatusCode(500, createdUser.Errors);
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("ConfirmEmail", "User",
                                                  new { userEmail = user.Email, token = HttpUtility.UrlEncode(token) },
                                                  protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(confirmationLink)}'>clicking here</a>.");

                var externalLoginInfo = await _signInManager.GetExternalLoginInfoAsync();
                if (externalLoginInfo != null)
                {
                    var externalResult = await _userManager.AddLoginAsync(user, externalLoginInfo);
                    if (!externalResult.Succeeded)
                    {
                        return BadRequest(externalResult.Errors);
                    }
                }

                return Ok(new NewUserDto
                {
                    UserName = user.Email,
                    Token = _tokenService.GenerateToken(user)
                });

            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, e.InnerException?.Message ?? e.Message);
            }
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userEmail, string token)
        {
            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Register", "User");
            }

            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                return NotFound($"Unable to load user with Email '{userEmail}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, HttpUtility.UrlDecode(token));
            if (result.Succeeded)
            {
                return Ok("Email confirmed successfully.");
            }

            return StatusCode(500, result.Errors);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                // If user doesn't exist or email is not confirmed, don't reveal this information for security reasons.
                return Ok("If the email exists, a reset password link has been sent.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "User",
                new { userEmail = user.Email, token = HttpUtility.UrlEncode(token) },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(forgotPasswordDto.Email, "Reset your password",
                $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(resetLink)}'>clicking here</a>.");

            return Ok("If the email exists, a reset password link has been sent.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                return Ok("Password reset successful.");
            }

            var decodedToken = HttpUtility.UrlDecode(resetPasswordDto.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.NewPassword);

            if (result.Succeeded)
            {
                return Ok("Password reset successful.");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(ModelState);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok("Logged out successfully.");
        }

        [HttpDelete("remove-external-login")]
        public async Task<IActionResult> RemoveExternalLogin([FromBody] RemoveExternalLoginDto removeExternalLoginDto)
        {
            var user = await _userManager.FindByEmailAsync(removeExternalLoginDto.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var logins = await _userManager.GetLoginsAsync(user);
            if (logins.Count == 1 && await _userManager.HasPasswordAsync(user) == false)
            {
                return BadRequest("You cannot remove the last external login without setting a password.");
            }

            var result = await _userManager.RemoveLoginAsync(user, removeExternalLoginDto.LoginProvider, removeExternalLoginDto.ProviderKey);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok("External login removed successfully.");
        }

        [HttpDelete("{email}")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"User with email '{email}' not found.");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Ok("User deleted successfully.");
            }

            return StatusCode(500, result.Errors);
        }

    }
}

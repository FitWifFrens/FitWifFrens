using FitWifFrens.Api.Dtos.User;
using FitWifFrens.Api.Services;
using FitWifFrens.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Encodings.Web;
using System.Web;

namespace FitWifFrens.Api.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signinManager;
        private readonly TokenService _tokenService;
        private readonly IEmailSender _emailSender;

        public UserController(UserManager<User> userManager, SignInManager<User> signinManager,
            TokenService tokenService, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signinManager = signinManager;
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

            var loginResult = await _signinManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!loginResult.Succeeded)
            {
                return Unauthorized("Username not found and/or password incorrect");
            }

            return Ok(
                new NewUserDto
                {
                    UserName = user.Email,
                    Token = _tokenService.GenerateToken(user)
                });
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

                if (createdUser.Succeeded)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmEmail", "User",
                                                      new { userEmail = user.Email, token = HttpUtility.UrlEncode(token) },
                                                      protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(confirmationLink)}'>clicking here</a>.");

                    return Ok(new NewUserDto
                    {
                        UserName = user.Email,
                        Token = _tokenService.GenerateToken(user)
                    });
                }

                return StatusCode(500, createdUser.Errors);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, e.InnerException?.Message ?? e.Message);
            }
        }

        [HttpGet("confirm email")]
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

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signinManager.SignOutAsync();
            return Ok("Logged out successfully.");
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

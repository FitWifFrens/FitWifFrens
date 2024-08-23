using System.ComponentModel.DataAnnotations;

namespace FitWifFrens.Api.Dtos.User
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}

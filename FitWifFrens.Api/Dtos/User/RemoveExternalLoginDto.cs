using System.ComponentModel.DataAnnotations;

namespace FitWifFrens.Api.Dtos.User
{
    public class RemoveExternalLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string LoginProvider { get; set; } = string.Empty;

        [Required]
        public string ProviderKey { get; set; } = string.Empty;
    }
}

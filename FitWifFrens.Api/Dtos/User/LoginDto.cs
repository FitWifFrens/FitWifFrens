﻿using System.ComponentModel.DataAnnotations;

namespace FitWifFrens.Api.Dtos.User
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

    }
}
using System.ComponentModel.DataAnnotations;

namespace TechNews.Auth.Api.Models;

/// <summary>
/// Confirm Email information
/// </summary>
public class ConfirmEmailRequestModel
{
    /// <summary>
    /// The user email
    /// </summary>
    [Required(ErrorMessage = "The {0} field is mandatory")]
    [MaxLength(256, ErrorMessage = "The {0} field must have a maximum length of {1} characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The email confirmation token
    /// </summary>
    public string? Token { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace UserService.API.DTOs;

public record RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [MinLength(2)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MinLength(2)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; init; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; init; }

    [Required]
    public string Country { get; init; } = string.Empty;
}

public record LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public UserResponse User { get; init; } = null!;
}

public record UserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public string Country { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsEmailVerified { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public record UpdateProfileRequest
{
    [Required]
    [MinLength(2)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MinLength(2)]
    public string LastName { get; init; } = string.Empty;

    [Phone]
    public string PhoneNumber { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;
}

public record ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;
}
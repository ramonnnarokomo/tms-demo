using System.ComponentModel.DataAnnotations;

namespace TmsDemo.Api.Contracts;

public sealed record CreateCarrierRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [EmailAddress, StringLength(200)]
    public string? Email { get; init; }
}

public sealed record CarrierResponse(int Id, string Name, string? Email, bool IsActive);

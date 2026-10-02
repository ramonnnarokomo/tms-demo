using System.ComponentModel.DataAnnotations;
using TmsDemo.Api.Domain;

namespace TmsDemo.Api.Contracts;

public sealed record CreateShipmentRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Origin { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Destination { get; init; } = string.Empty;

    [Range(0.01, 40000)]
    public decimal WeightKg { get; init; }

    [Range(1, int.MaxValue)]
    public int CarrierId { get; init; }

    [Required]
    public DateTime? EstimatedDeliveryUtc { get; init; }
}

public sealed record ChangeShipmentStatusRequest
{
    [Required]
    public ShipmentStatus? Status { get; init; }

    [StringLength(500)]
    public string? Note { get; init; }
}

/// <summary>Filters and paging for GET /api/shipments.</summary>
public sealed record ShipmentQuery
{
    public ShipmentStatus? Status { get; init; }
    public int? CarrierId { get; init; }
    public string? Destination { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record ShipmentSummary(
    int Id,
    string TrackingNumber,
    string Origin,
    string Destination,
    ShipmentStatus Status,
    string CarrierName,
    DateTime EstimatedDeliveryUtc,
    DateTime CreatedAtUtc);

public sealed record ShipmentEventResponse(ShipmentStatus Status, DateTime OccurredAtUtc, string? Note);

public sealed record ShipmentDetails(
    int Id,
    string TrackingNumber,
    string Origin,
    string Destination,
    decimal WeightKg,
    ShipmentStatus Status,
    CarrierResponse Carrier,
    DateTime CreatedAtUtc,
    DateTime EstimatedDeliveryUtc,
    DateTime? DeliveredAtUtc,
    IReadOnlyList<ShipmentEventResponse> Events);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

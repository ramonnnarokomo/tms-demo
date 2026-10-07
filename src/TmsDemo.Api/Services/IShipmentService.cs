using TmsDemo.Api.Contracts;

namespace TmsDemo.Api.Services;

public interface IShipmentService
{
    Task<PagedResult<ShipmentSummary>> SearchAsync(ShipmentQuery query, CancellationToken ct);
    Task<ShipmentDetails> GetByIdAsync(int id, CancellationToken ct);
    Task<ShipmentDetails> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct);
    Task<ShipmentDetails> CreateAsync(CreateShipmentRequest request, CancellationToken ct);
    Task<ShipmentDetails> ChangeStatusAsync(int id, ChangeShipmentStatusRequest request, CancellationToken ct);
    Task<ShipmentDetails> RescheduleDeliveryAsync(int id, RescheduleDeliveryRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

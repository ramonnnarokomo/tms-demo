using TmsDemo.Api.Contracts;

namespace TmsDemo.Api.Services;

public interface ICarrierService
{
    Task<IReadOnlyList<CarrierResponse>> GetAllAsync(CancellationToken ct);
    Task<CarrierResponse> GetByIdAsync(int id, CancellationToken ct);
    Task<CarrierResponse> CreateAsync(CreateCarrierRequest request, CancellationToken ct);
    Task<CarrierResponse> DeactivateAsync(int id, CancellationToken ct);
    Task<CarrierResponse> ActivateAsync(int id, CancellationToken ct);
}

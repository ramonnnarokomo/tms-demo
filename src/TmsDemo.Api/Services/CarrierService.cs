using Microsoft.EntityFrameworkCore;
using TmsDemo.Api.Contracts;
using TmsDemo.Api.Data;
using TmsDemo.Api.Domain;

namespace TmsDemo.Api.Services;

public sealed class CarrierService(TmsDbContext db) : ICarrierService
{
    public async Task<IReadOnlyList<CarrierResponse>> GetAllAsync(CancellationToken ct) =>
        await db.Carriers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CarrierResponse(c.Id, c.Name, c.Email, c.IsActive))
            .ToListAsync(ct);

    public async Task<CarrierResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var carrier = await FindAsync(id, ct);
        return carrier.ToResponse();
    }

    public async Task<CarrierResponse> CreateAsync(CreateCarrierRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();

        if (await db.Carriers.AnyAsync(c => c.Name == name, ct))
            throw new DomainException($"A carrier named '{name}' already exists.");

        var carrier = new Carrier(name, request.Email);
        db.Carriers.Add(carrier);
        await db.SaveChangesAsync(ct);

        return carrier.ToResponse();
    }

    public async Task<CarrierResponse> DeactivateAsync(int id, CancellationToken ct)
    {
        var carrier = await FindAsync(id, ct);

        carrier.Deactivate();
        await db.SaveChangesAsync(ct);

        return carrier.ToResponse();
    }

    public async Task<CarrierResponse> ActivateAsync(int id, CancellationToken ct)
    {
        var carrier = await FindAsync(id, ct);

        carrier.Activate();
        await db.SaveChangesAsync(ct);

        return carrier.ToResponse();
    }

    private async Task<Carrier> FindAsync(int id, CancellationToken ct) =>
        await db.Carriers.FirstOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new NotFoundException($"Carrier {id} was not found.");
}

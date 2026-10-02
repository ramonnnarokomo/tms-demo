using Microsoft.EntityFrameworkCore;
using TmsDemo.Api.Contracts;
using TmsDemo.Api.Data;
using TmsDemo.Api.Domain;

namespace TmsDemo.Api.Services;

public sealed class ShipmentService(TmsDbContext db, TimeProvider timeProvider) : IShipmentService
{
    public async Task<PagedResult<ShipmentSummary>> SearchAsync(ShipmentQuery query, CancellationToken ct)
    {
        IQueryable<Shipment> shipments = db.Shipments.AsNoTracking();

        if (query.Status is { } status)
            shipments = shipments.Where(s => s.Status == status);

        if (query.CarrierId is { } carrierId)
            shipments = shipments.Where(s => s.CarrierId == carrierId);

        if (!string.IsNullOrWhiteSpace(query.Destination))
        {
            var pattern = $"%{query.Destination.Trim()}%";
            shipments = shipments.Where(s => EF.Functions.Like(s.Destination, pattern));
        }

        var totalCount = await shipments.CountAsync(ct);

        var items = await shipments
            .OrderByDescending(s => s.CreatedAtUtc)
            .ThenByDescending(s => s.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new ShipmentSummary(
                s.Id,
                s.TrackingNumber,
                s.Origin,
                s.Destination,
                s.Status,
                s.Carrier!.Name,
                s.EstimatedDeliveryUtc,
                s.CreatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<ShipmentSummary>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<ShipmentDetails> GetByIdAsync(int id, CancellationToken ct)
    {
        var shipment = await LoadShipmentAsync(id, ct);
        return shipment.ToDetails();
    }

    public async Task<ShipmentDetails> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct)
    {
        var normalized = trackingNumber.Trim().ToUpperInvariant();

        var shipment = await db.Shipments
            .AsNoTracking()
            .Include(s => s.Carrier)
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.TrackingNumber == normalized, ct)
            ?? throw new NotFoundException($"Shipment with tracking number '{normalized}' was not found.");

        return shipment.ToDetails();
    }

    public async Task<ShipmentDetails> CreateAsync(CreateShipmentRequest request, CancellationToken ct)
    {
        var carrier = await db.Carriers.FirstOrDefaultAsync(c => c.Id == request.CarrierId, ct)
            ?? throw new NotFoundException($"Carrier {request.CarrierId} was not found.");

        var now = UtcNow();

        var shipment = new Shipment(
            TrackingNumberGenerator.Next(now),
            request.Origin,
            request.Destination,
            request.WeightKg,
            carrier,
            request.EstimatedDeliveryUtc!.Value.ToUniversalTime(),
            now);

        db.Shipments.Add(shipment);
        await db.SaveChangesAsync(ct);

        return shipment.ToDetails();
    }

    public async Task<ShipmentDetails> ChangeStatusAsync(int id, ChangeShipmentStatusRequest request, CancellationToken ct)
    {
        var shipment = await LoadShipmentAsync(id, ct);

        shipment.ChangeStatus(request.Status!.Value, UtcNow(), request.Note);
        await db.SaveChangesAsync(ct);

        return shipment.ToDetails();
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var shipment = await LoadShipmentAsync(id, ct);

        shipment.EnsureCanBeDeleted();
        db.Shipments.Remove(shipment);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Shipment> LoadShipmentAsync(int id, CancellationToken ct) =>
        await db.Shipments
            .Include(s => s.Carrier)
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
        ?? throw new NotFoundException($"Shipment {id} was not found.");

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
}

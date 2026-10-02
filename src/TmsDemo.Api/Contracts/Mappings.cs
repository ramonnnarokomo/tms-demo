using TmsDemo.Api.Domain;

namespace TmsDemo.Api.Contracts;

/// <summary>Entity -> DTO mapping. The API never returns EF Core entities directly.</summary>
public static class Mappings
{
    public static CarrierResponse ToResponse(this Carrier carrier) =>
        new(carrier.Id, carrier.Name, carrier.Email, carrier.IsActive);

    public static ShipmentDetails ToDetails(this Shipment shipment)
    {
        var carrier = shipment.Carrier
            ?? throw new InvalidOperationException("Carrier must be loaded to map a shipment.");

        var events = shipment.Events
            .OrderBy(e => e.OccurredAtUtc)
            .ThenBy(e => e.Id)
            .Select(e => new ShipmentEventResponse(e.Status, e.OccurredAtUtc, e.Note))
            .ToList();

        return new ShipmentDetails(
            shipment.Id,
            shipment.TrackingNumber,
            shipment.Origin,
            shipment.Destination,
            shipment.WeightKg,
            shipment.Status,
            carrier.ToResponse(),
            shipment.CreatedAtUtc,
            shipment.EstimatedDeliveryUtc,
            shipment.DeliveredAtUtc,
            events);
    }
}

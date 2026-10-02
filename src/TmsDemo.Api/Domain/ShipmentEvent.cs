namespace TmsDemo.Api.Domain;

/// <summary>One entry of a shipment's tracking history.</summary>
public sealed class ShipmentEvent
{
    // Used by EF Core when loading entities from the database.
    private ShipmentEvent()
    {
    }

    internal ShipmentEvent(ShipmentStatus status, DateTime occurredAtUtc, string? note)
    {
        Status = status;
        OccurredAtUtc = occurredAtUtc;
        Note = note;
    }

    public int Id { get; private set; }
    public int ShipmentId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? Note { get; private set; }
}

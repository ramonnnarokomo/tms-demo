namespace TmsDemo.Api.Domain;

/// <summary>
/// A shipment moving goods from an origin to a destination with a given carrier.
/// Business rules (valid data, allowed status changes, tracking history) live here,
/// so controllers and services can't put a shipment into an invalid state.
/// </summary>
public sealed class Shipment
{
    public const decimal MaxWeightKg = 40_000m;

    // Status lifecycle:
    // Pending -> InTransit | Cancelled
    // InTransit -> Delivered | Incident
    // Incident -> InTransit | Cancelled
    // Delivered and Cancelled are final states.
    private static readonly Dictionary<ShipmentStatus, ShipmentStatus[]> AllowedTransitions = new()
    {
        [ShipmentStatus.Pending] = [ShipmentStatus.InTransit, ShipmentStatus.Cancelled],
        [ShipmentStatus.InTransit] = [ShipmentStatus.Delivered, ShipmentStatus.Incident],
        [ShipmentStatus.Incident] = [ShipmentStatus.InTransit, ShipmentStatus.Cancelled],
        [ShipmentStatus.Delivered] = [],
        [ShipmentStatus.Cancelled] = [],
    };

    private readonly List<ShipmentEvent> _events = [];

    // Used by EF Core when loading entities from the database.
    private Shipment()
    {
    }

    public Shipment(
        string trackingNumber,
        string origin,
        string destination,
        decimal weightKg,
        Carrier carrier,
        DateTime estimatedDeliveryUtc,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(carrier);

        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new DomainException("Tracking number is required.");

        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
            throw new DomainException("Origin and destination are required.");

        if (string.Equals(origin.Trim(), destination.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Origin and destination must be different.");

        if (weightKg <= 0 || weightKg > MaxWeightKg)
            throw new DomainException($"Weight must be greater than 0 and at most {MaxWeightKg} kg.");

        if (!carrier.IsActive)
            throw new DomainException($"Carrier '{carrier.Name}' is not active.");

        if (estimatedDeliveryUtc <= nowUtc)
            throw new DomainException("Estimated delivery must be in the future.");

        TrackingNumber = trackingNumber;
        Origin = origin.Trim();
        Destination = destination.Trim();
        WeightKg = weightKg;
        Carrier = carrier;
        CarrierId = carrier.Id;
        EstimatedDeliveryUtc = estimatedDeliveryUtc;
        CreatedAtUtc = nowUtc;
        Status = ShipmentStatus.Pending;

        _events.Add(new ShipmentEvent(ShipmentStatus.Pending, nowUtc, "Shipment registered"));
    }

    public int Id { get; private set; }
    public string TrackingNumber { get; private set; } = string.Empty;
    public string Origin { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public decimal WeightKg { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public int CarrierId { get; private set; }
    public Carrier? Carrier { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime EstimatedDeliveryUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }

    /// <summary>Tracking history (track &amp; trace). Read-only from outside: only status changes add events.</summary>
    public IReadOnlyCollection<ShipmentEvent> Events => _events.AsReadOnly();

    public bool CanTransitionTo(ShipmentStatus newStatus) =>
        AllowedTransitions[Status].Contains(newStatus);

    public void ChangeStatus(ShipmentStatus newStatus, DateTime nowUtc, string? note = null)
    {
        if (newStatus == Status)
            throw new DomainException($"Shipment is already {Status}.");

        if (!CanTransitionTo(newStatus))
            throw new DomainException($"Cannot change status from {Status} to {newStatus}.");

        if (newStatus == ShipmentStatus.Incident && string.IsNullOrWhiteSpace(note))
            throw new DomainException("A note is required when reporting an incident.");

        Status = newStatus;

        if (newStatus == ShipmentStatus.Delivered)
            DeliveredAtUtc = nowUtc;

        _events.Add(new ShipmentEvent(newStatus, nowUtc, string.IsNullOrWhiteSpace(note) ? null : note.Trim()));
    }

    public void EnsureCanBeDeleted()
    {
        if (Status != ShipmentStatus.Pending)
            throw new DomainException("Only pending shipments can be deleted. Cancel the shipment instead.");
    }
}

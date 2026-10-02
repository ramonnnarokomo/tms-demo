using TmsDemo.Api.Domain;

namespace TmsDemo.Api.Tests.Domain;

/// <summary>Pure unit tests of the business rules. No database, no HTTP.</summary>
public sealed class ShipmentTests
{
    private static readonly DateTime Now = new(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_shipment_starts_pending_with_one_tracking_event()
    {
        var shipment = NewShipment();

        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
        Assert.Equal(Now, shipment.CreatedAtUtc);
        var registered = Assert.Single(shipment.Events);
        Assert.Equal(ShipmentStatus.Pending, registered.Status);
    }

    [Fact]
    public void Origin_and_destination_are_trimmed()
    {
        var shipment = NewShipment(origin: "  Madrid ", destination: " Valencia  ");

        Assert.Equal("Madrid", shipment.Origin);
        Assert.Equal("Valencia", shipment.Destination);
    }

    [Theory]
    [InlineData("Madrid", "Madrid")]
    [InlineData("Madrid", "  madrid ")]
    public void Origin_and_destination_must_be_different(string origin, string destination)
    {
        var error = Assert.Throws<DomainException>(() => NewShipment(origin: origin, destination: destination));

        Assert.Contains("must be different", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(40_000.01)]
    public void Weight_must_be_within_limits(double weightKg)
    {
        Assert.Throws<DomainException>(() => NewShipment(weightKg: (decimal)weightKg));
    }

    [Fact]
    public void Inactive_carrier_cannot_take_new_shipments()
    {
        var carrier = new Carrier("Cantábrico Cargo", null);
        carrier.Deactivate();

        var error = Assert.Throws<DomainException>(() => NewShipment(carrier: carrier));

        Assert.Contains("not active", error.Message);
    }

    [Fact]
    public void Estimated_delivery_must_be_in_the_future()
    {
        Assert.Throws<DomainException>(() => NewShipment(estimatedDeliveryUtc: Now.AddMinutes(-1)));
    }

    [Theory]
    [InlineData(ShipmentStatus.Pending, ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Pending, ShipmentStatus.Cancelled)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Incident)]
    [InlineData(ShipmentStatus.Incident, ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Incident, ShipmentStatus.Cancelled)]
    public void Allowed_transitions_change_status_and_add_an_event(ShipmentStatus from, ShipmentStatus to)
    {
        var shipment = ShipmentIn(from);
        var eventsBefore = shipment.Events.Count;

        shipment.ChangeStatus(to, Now.AddHours(5), "note");

        Assert.Equal(to, shipment.Status);
        Assert.Equal(eventsBefore + 1, shipment.Events.Count);
    }

    [Theory]
    [InlineData(ShipmentStatus.Pending, ShipmentStatus.Delivered)]
    [InlineData(ShipmentStatus.Pending, ShipmentStatus.Incident)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Pending)]
    [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Cancelled)]
    [InlineData(ShipmentStatus.Delivered, ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Cancelled, ShipmentStatus.Pending)]
    public void Forbidden_transitions_are_rejected(ShipmentStatus from, ShipmentStatus to)
    {
        var shipment = ShipmentIn(from);

        Assert.Throws<DomainException>(() => shipment.ChangeStatus(to, Now.AddHours(5), "note"));
        Assert.Equal(from, shipment.Status);
    }

    [Fact]
    public void Changing_to_the_same_status_is_rejected()
    {
        var shipment = NewShipment();

        Assert.Throws<DomainException>(() => shipment.ChangeStatus(ShipmentStatus.Pending, Now.AddHours(1)));
    }

    [Theory]
    [InlineData((string?)null)]
    [InlineData("   ")]
    public void Reporting_an_incident_requires_a_note(string? note)
    {
        var shipment = ShipmentIn(ShipmentStatus.InTransit);

        var error = Assert.Throws<DomainException>(() => shipment.ChangeStatus(ShipmentStatus.Incident, Now.AddHours(2), note));

        Assert.Contains("note is required", error.Message);
    }

    [Fact]
    public void Delivering_sets_the_delivery_date()
    {
        var shipment = ShipmentIn(ShipmentStatus.InTransit);
        var deliveredAt = Now.AddDays(1);

        shipment.ChangeStatus(ShipmentStatus.Delivered, deliveredAt, "Signed by receiver");

        Assert.Equal(deliveredAt, shipment.DeliveredAtUtc);
        Assert.Equal("Signed by receiver", shipment.Events.Last().Note);
    }

    [Fact]
    public void Only_pending_shipments_can_be_deleted()
    {
        NewShipment().EnsureCanBeDeleted(); // does not throw

        var inTransit = ShipmentIn(ShipmentStatus.InTransit);
        Assert.Throws<DomainException>(() => inTransit.EnsureCanBeDeleted());
    }

    [Fact]
    public void Tracking_numbers_have_the_expected_format()
    {
        var trackingNumber = TrackingNumberGenerator.Next(Now);

        Assert.StartsWith("TMS261002", trackingNumber);
        Assert.Equal(15, trackingNumber.Length);
    }

    private static Shipment NewShipment(
        string origin = "Madrid",
        string destination = "Valencia",
        decimal weightKg = 500m,
        Carrier? carrier = null,
        DateTime? estimatedDeliveryUtc = null) =>
        new(
            "TMS261002TEST01",
            origin,
            destination,
            weightKg,
            carrier ?? new Carrier("Transportes Meseta", "ops@meseta.example"),
            estimatedDeliveryUtc ?? Now.AddDays(2),
            Now);

    /// <summary>Builds a shipment and walks it through valid transitions until it reaches the given status.</summary>
    private static Shipment ShipmentIn(ShipmentStatus status)
    {
        var shipment = NewShipment();
        var at = Now.AddMinutes(1);

        ShipmentStatus[] path = status switch
        {
            ShipmentStatus.Pending => [],
            ShipmentStatus.InTransit => [ShipmentStatus.InTransit],
            ShipmentStatus.Delivered => [ShipmentStatus.InTransit, ShipmentStatus.Delivered],
            ShipmentStatus.Incident => [ShipmentStatus.InTransit, ShipmentStatus.Incident],
            ShipmentStatus.Cancelled => [ShipmentStatus.Cancelled],
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

        foreach (var step in path)
        {
            shipment.ChangeStatus(step, at, "setup");
            at = at.AddMinutes(1);
        }

        return shipment;
    }
}

using TmsDemo.Api.Domain;

namespace TmsDemo.Api.Tests.Domain;

public sealed class CarrierTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_carrier_is_active_with_trimmed_data()
    {
        var carrier = new Carrier("  Levante Express ", "  trafico@levante.example ");

        Assert.True(carrier.IsActive);
        Assert.Equal("Levante Express", carrier.Name);
        Assert.Equal("trafico@levante.example", carrier.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Carrier_name_is_required(string name)
    {
        Assert.Throws<DomainException>(() => new Carrier(name, null));
    }

    [Fact]
    public void Blank_email_is_stored_as_null()
    {
        Assert.Null(new Carrier("Levante Express", "  ").Email);
    }

    [Fact]
    public void Reactivated_carrier_can_take_new_shipments_again()
    {
        var carrier = new Carrier("Cantábrico Cargo", null);
        carrier.Deactivate();
        Assert.False(carrier.IsActive);

        carrier.Activate();

        Assert.True(carrier.IsActive);
        var shipment = new Shipment("TMS261007TEST01", "Bilbao", "Oviedo", 100m, carrier, Now.AddDays(1), Now);
        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
    }
}

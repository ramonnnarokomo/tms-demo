using TmsDemo.Api.Domain;

namespace TmsDemo.Api.Data;

/// <summary>Adds a few sample shipments so the API isn't empty the first time you open it.</summary>
public static class DemoDataSeeder
{
    public static void Seed(TmsDbContext db, TimeProvider timeProvider)
    {
        if (db.Shipments.Any())
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var meseta = db.Carriers.Single(c => c.Id == 1);
        var levante = db.Carriers.Single(c => c.Id == 2);

        var pending = new Shipment(
            TrackingNumberGenerator.Next(now), "Madrid", "Barcelona", 1250.5m, meseta, now.AddDays(2), now);

        var inTransit = new Shipment(
            TrackingNumberGenerator.Next(now), "Valencia", "Sevilla", 820m, levante, now.AddDays(2), now.AddHours(-6));
        inTransit.ChangeStatus(ShipmentStatus.InTransit, now.AddHours(-4), "Picked up at Valencia hub");

        var delivered = new Shipment(
            TrackingNumberGenerator.Next(now), "Bilbao", "Zaragoza", 300m, meseta, now.AddHours(-1), now.AddDays(-1));
        delivered.ChangeStatus(ShipmentStatus.InTransit, now.AddHours(-20));
        delivered.ChangeStatus(ShipmentStatus.Delivered, now.AddHours(-2), "Signed by receiver");

        db.Shipments.AddRange(pending, inTransit, delivered);
        db.SaveChanges();
    }
}

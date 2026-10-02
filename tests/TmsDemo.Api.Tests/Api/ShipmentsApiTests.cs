using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TmsDemo.Api.Contracts;
using TmsDemo.Api.Domain;

namespace TmsDemo.Api.Tests.Api;

/// <summary>End-to-end tests: HTTP request -> controller -> service -> EF Core -> SQLite and back.</summary>
public sealed class ShipmentsApiTests(TmsApiFactory factory) : IClassFixture<TmsApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Created_shipment_can_be_tracked_by_its_tracking_number()
    {
        var created = await CreateShipmentAsync(destination: "Valencia");

        Assert.Equal(ShipmentStatus.Pending, created.Status);
        Assert.Equal("Transportes Meseta", created.Carrier.Name);
        Assert.Single(created.Events);

        var tracked = await _client.GetFromJsonAsync<ShipmentDetails>(
            $"/api/shipments/tracking/{created.TrackingNumber.ToLowerInvariant()}", Json);

        Assert.Equal(created.Id, tracked!.Id);
    }

    [Fact]
    public async Task Full_lifecycle_keeps_the_tracking_history()
    {
        var created = await CreateShipmentAsync();

        await ChangeStatusAsync(created.Id, ShipmentStatus.InTransit, "Picked up");
        await ChangeStatusAsync(created.Id, ShipmentStatus.Incident, "Truck breakdown near Zaragoza");
        await ChangeStatusAsync(created.Id, ShipmentStatus.InTransit, "Replacement truck");
        var delivered = await ChangeStatusAsync(created.Id, ShipmentStatus.Delivered, "Signed by receiver");

        Assert.Equal(ShipmentStatus.Delivered, delivered.Status);
        Assert.NotNull(delivered.DeliveredAtUtc);
        var expectedHistory = new[]
        {
            ShipmentStatus.Pending,
            ShipmentStatus.InTransit,
            ShipmentStatus.Incident,
            ShipmentStatus.InTransit,
            ShipmentStatus.Delivered,
        };
        Assert.Equal(expectedHistory, delivered.Events.Select(e => e.Status).ToArray());
    }

    [Fact]
    public async Task Invalid_transition_returns_422_problem_details()
    {
        var created = await CreateShipmentAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/shipments/{created.Id}/status", new { status = "Delivered" }, Json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Contains("Cannot change status", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Inactive_carrier_returns_422()
    {
        // Carrier 3 is seeded as inactive.
        var response = await PostShipmentAsync(carrierId: 3);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_carrier_returns_404()
    {
        var response = await PostShipmentAsync(carrierId: 999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_payload_returns_400()
    {
        var response = await PostShipmentAsync(weightKg: 0);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Only_pending_shipments_can_be_deleted()
    {
        var pending = await CreateShipmentAsync();
        var inTransit = await CreateShipmentAsync();
        await ChangeStatusAsync(inTransit.Id, ShipmentStatus.InTransit);

        var deleteInTransit = await _client.DeleteAsync($"/api/shipments/{inTransit.Id}");
        var deletePending = await _client.DeleteAsync($"/api/shipments/{pending.Id}");
        var getDeleted = await _client.GetAsync($"/api/shipments/{pending.Id}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, deleteInTransit.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deletePending.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
    }

    [Fact]
    public async Task Search_filters_by_destination_and_status()
    {
        var destination = $"Teruel-{Guid.NewGuid():N}"[..20];
        var first = await CreateShipmentAsync(destination: destination);
        await CreateShipmentAsync(destination: destination);
        await ChangeStatusAsync(first.Id, ShipmentStatus.InTransit);

        var result = await _client.GetFromJsonAsync<PagedResult<ShipmentSummary>>(
            $"/api/shipments?destination={destination}&status=InTransit", Json);

        var item = Assert.Single(result!.Items);
        Assert.Equal(first.Id, item.Id);
        Assert.Equal(1, result.TotalCount);
    }

    private Task<HttpResponseMessage> PostShipmentAsync(
        string destination = "Valencia", decimal weightKg = 500m, int carrierId = 1) =>
        _client.PostAsJsonAsync("/api/shipments", new
        {
            origin = "Madrid",
            destination,
            weightKg,
            carrierId,
            estimatedDeliveryUtc = DateTime.UtcNow.AddDays(3),
        }, Json);

    private async Task<ShipmentDetails> CreateShipmentAsync(string destination = "Valencia")
    {
        var response = await PostShipmentAsync(destination);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ShipmentDetails>(Json))!;
    }

    private async Task<ShipmentDetails> ChangeStatusAsync(int id, ShipmentStatus status, string? note = null)
    {
        var response = await _client.PatchAsJsonAsync($"/api/shipments/{id}/status", new { status, note }, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShipmentDetails>(Json))!;
    }
}

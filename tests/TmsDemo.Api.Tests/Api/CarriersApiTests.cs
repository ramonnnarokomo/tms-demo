using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TmsDemo.Api.Contracts;

namespace TmsDemo.Api.Tests.Api;

public sealed class CarriersApiTests(TmsApiFactory factory) : IClassFixture<TmsApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Deactivated_carrier_takes_shipments_again_after_reactivation()
    {
        var created = await _client.PostAsJsonAsync("/api/carriers", new { name = $"Carrier {Guid.NewGuid():N}"[..20] }, Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var carrier = (await created.Content.ReadFromJsonAsync<CarrierResponse>(Json))!;

        var deactivated = await PostForCarrierAsync($"/api/carriers/{carrier.Id}/deactivate");
        Assert.False(deactivated.IsActive);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PostShipmentAsync(carrier.Id)).StatusCode);

        var activated = await PostForCarrierAsync($"/api/carriers/{carrier.Id}/activate");
        Assert.True(activated.IsActive);
        Assert.Equal(HttpStatusCode.Created, (await PostShipmentAsync(carrier.Id)).StatusCode);
    }

    [Fact]
    public async Task Activating_an_unknown_carrier_returns_404()
    {
        var response = await _client.PostAsync("/api/carriers/999/activate", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<CarrierResponse> PostForCarrierAsync(string url)
    {
        var response = await _client.PostAsync(url, content: null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CarrierResponse>(Json))!;
    }

    private Task<HttpResponseMessage> PostShipmentAsync(int carrierId) =>
        _client.PostAsJsonAsync("/api/shipments", new
        {
            origin = "Bilbao",
            destination = "Oviedo",
            weightKg = 100m,
            carrierId,
            estimatedDeliveryUtc = DateTime.UtcNow.AddDays(2),
        }, Json);
}

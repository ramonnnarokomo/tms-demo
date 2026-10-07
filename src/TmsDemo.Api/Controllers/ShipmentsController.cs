using Microsoft.AspNetCore.Mvc;
using TmsDemo.Api.Contracts;
using TmsDemo.Api.Services;

namespace TmsDemo.Api.Controllers;

[ApiController]
[Route("api/shipments")]
[Produces("application/json")]
public sealed class ShipmentsController(IShipmentService shipments) : ControllerBase
{
    /// <summary>Search shipments with optional filters (status, carrier, destination) and paging.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ShipmentSummary>>> Search([FromQuery] ShipmentQuery query, CancellationToken ct) =>
        Ok(await shipments.SearchAsync(query, ct));

    /// <summary>Get a shipment with its carrier and full tracking history.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShipmentDetails>> GetById(int id, CancellationToken ct) =>
        Ok(await shipments.GetByIdAsync(id, ct));

    /// <summary>Track a shipment by its tracking number (what a customer would use).</summary>
    [HttpGet("tracking/{trackingNumber}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShipmentDetails>> GetByTrackingNumber(string trackingNumber, CancellationToken ct) =>
        Ok(await shipments.GetByTrackingNumberAsync(trackingNumber, ct));

    /// <summary>Register a new shipment. It starts as Pending.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ShipmentDetails>> Create(CreateShipmentRequest request, CancellationToken ct)
    {
        var created = await shipments.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Move a shipment to a new status (InTransit, Delivered, Incident, Cancelled).</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ShipmentDetails>> ChangeStatus(int id, ChangeShipmentStatusRequest request, CancellationToken ct) =>
        Ok(await shipments.ChangeStatusAsync(id, request, ct));

    /// <summary>Change the estimated delivery date (not allowed for Delivered or Cancelled shipments).</summary>
    [HttpPatch("{id:int}/eta")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ShipmentDetails>> RescheduleDelivery(int id, RescheduleDeliveryRequest request, CancellationToken ct) =>
        Ok(await shipments.RescheduleDeliveryAsync(id, request, ct));

    /// <summary>Delete a shipment. Only allowed while it is still Pending.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await shipments.DeleteAsync(id, ct);
        return NoContent();
    }
}

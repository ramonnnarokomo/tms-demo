using Microsoft.AspNetCore.Mvc;
using TmsDemo.Api.Contracts;
using TmsDemo.Api.Services;

namespace TmsDemo.Api.Controllers;

[ApiController]
[Route("api/carriers")]
[Produces("application/json")]
public sealed class CarriersController(ICarrierService carriers) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CarrierResponse>>> GetAll(CancellationToken ct) =>
        Ok(await carriers.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarrierResponse>> GetById(int id, CancellationToken ct) =>
        Ok(await carriers.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CarrierResponse>> Create(CreateCarrierRequest request, CancellationToken ct)
    {
        var created = await carriers.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Deactivate a carrier so no new shipments can be assigned to it.</summary>
    [HttpPost("{id:int}/deactivate")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarrierResponse>> Deactivate(int id, CancellationToken ct) =>
        Ok(await carriers.DeactivateAsync(id, ct));
}

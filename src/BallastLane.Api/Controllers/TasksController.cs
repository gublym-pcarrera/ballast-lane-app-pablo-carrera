using BallastLane.Api.Security;
using BallastLane.Application.Contracts;
using BallastLane.Application.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BallastLane.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(ITaskService tasks, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        return Ok(await tasks.ListAsync(currentUser.UserId, status, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await tasks.GetAsync(currentUser.UserId, id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(UpsertTaskRequest request, CancellationToken cancellationToken)
    {
        var created = await tasks.CreateAsync(currentUser.UserId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(Guid id, UpsertTaskRequest request, CancellationToken cancellationToken)
    {
        return Ok(await tasks.UpdateAsync(currentUser.UserId, id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await tasks.DeleteAsync(currentUser.UserId, id, cancellationToken);
        return NoContent();
    }
}

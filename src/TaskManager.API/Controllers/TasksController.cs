using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Common;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Application.Tasks.Interfaces;

namespace TaskManager.API.Controllers;

/// <summary>
/// CRUD and workflow endpoints for the authenticated user's tasks. Every action operates only
/// on tasks owned by the caller; the user id is taken from the validated JWT, never the request body.
/// </summary>
[ApiController]
[Route("api/tasks")]
[Authorize]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>Lists the caller's tasks, optionally filtered by status, priority, completion or search term.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetTasks([FromQuery] TaskQueryParameters query, CancellationToken ct)
    {
        var tasks = await _taskService.GetTasksAsync(User.GetUserId(), query, ct);
        return Ok(tasks);
    }

    /// <summary>Returns a single task by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetTask(Guid id, CancellationToken ct)
    {
        var task = await _taskService.GetByIdAsync(User.GetUserId(), id, ct);
        return Ok(task);
    }

    /// <summary>Creates a new task.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto dto, CancellationToken ct)
    {
        var created = await _taskService.CreateAsync(User.GetUserId(), dto, ct);
        return CreatedAtAction(nameof(GetTask), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing task (partial update — only supplied fields change).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> UpdateTask(Guid id, [FromBody] UpdateTaskDto dto, CancellationToken ct)
    {
        var updated = await _taskService.UpdateAsync(User.GetUserId(), id, dto, ct);
        return Ok(updated);
    }

    /// <summary>Marks a task complete or incomplete.</summary>
    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> SetCompletion(Guid id, [FromQuery] bool isCompleted, CancellationToken ct)
    {
        var updated = await _taskService.SetCompletionAsync(User.GetUserId(), id, isCompleted, ct);
        return Ok(updated);
    }

    /// <summary>Persists a new drag-and-drop ordering for the caller's tasks.</summary>
    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reorder([FromBody] ReorderTasksDto dto, CancellationToken ct)
    {
        await _taskService.ReorderAsync(User.GetUserId(), dto, ct);
        return NoContent();
    }

    /// <summary>Deletes a task.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken ct)
    {
        await _taskService.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}

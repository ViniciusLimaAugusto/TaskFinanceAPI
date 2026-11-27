using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFinanceAPI.Data;
using TaskFinanceAPI.DTOs;
using TaskFinanceAPI.Services;

namespace TaskFinanceAPI.Controllers;

[ApiController]
[Route("api/Tarefas")]
public class TarefasController(AppDbContext database, TaskService tasks) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TaskResponse>>> GetTasks(CancellationToken cancellationToken) =>
        await tasks.Query().ToListAsync(cancellationToken);

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetTask(int id, CancellationToken cancellationToken)
    {
        var task = await tasks.Query(id).FirstOrDefaultAsync(cancellationToken);
        return task is null ? NotFound() : task;
    }

    [HttpPost]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskResponse>> CreateTask(TaskRequest request, CancellationToken cancellationToken)
    {
        var task = await tasks.CreateAsync(request, cancellationToken);
        return task is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "The specified user was not found.")
            : CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTask(int id, TaskRequest request, CancellationToken cancellationToken)
    {
        var result = await tasks.UpdateAsync(id, request, cancellationToken);
        return result switch
        {
            TaskUpdateResult.TaskMissing => NotFound(),
            TaskUpdateResult.OwnerMissing =>
                Problem(statusCode: StatusCodes.Status404NotFound, title: "The specified user was not found."),
            _ => NoContent()
        };
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTask(int id, CancellationToken cancellationToken)
    {
        var task = await database.Tarefas.FindAsync([id], cancellationToken);
        if (task is not null)
        {
            database.Tarefas.Remove(task);
            await database.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }
}

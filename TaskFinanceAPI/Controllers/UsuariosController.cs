using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFinanceAPI.Data;
using TaskFinanceAPI.DTOs;
using TaskFinanceAPI.Models;
using ApiTaskStatus = TaskFinanceAPI.DTOs.TaskStatus;

namespace TaskFinanceAPI.Controllers;

[ApiController]
[Route("api/Usuarios")]
public class UsuariosController(AppDbContext database) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetUsers(CancellationToken cancellationToken) =>
        await QueryUsers().ToListAsync(cancellationToken);

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUser(int id, CancellationToken cancellationToken)
    {
        var user = await QueryUsers(id).FirstOrDefaultAsync(cancellationToken);
        return user is null ? NotFound() : user;
    }

    [HttpPost]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserResponse>> CreateUser(
        UserRequest request, CancellationToken cancellationToken)
    {
        var user = new Usuario { Nome = request.Name, Saldo = request.Balance };
        database.Usuarios.Add(user);
        await database.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id },
            new UserResponse(user.Id, user.Nome, user.Saldo, []));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser(int id, UserRequest request, CancellationToken cancellationToken)
    {
        var user = await database.Usuarios.FindAsync([id], cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.Nome = request.Name;
        user.Saldo = request.Balance;
        await database.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var user = await database.Usuarios.FindAsync([id], cancellationToken);
        if (user is not null)
        {
            database.Usuarios.Remove(user);
            await database.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    private IQueryable<UserResponse> QueryUsers(int? id = null)
    {
        var query = database.Usuarios.AsNoTracking();
        if (id is int userId)
        {
            query = query.Where(user => user.Id == userId);
        }

        return query.OrderBy(user => user.Id)
            .Select(user => new UserResponse(user.Id, user.Nome, user.Saldo,
                user.Tarefas.OrderBy(task => task.Id)
                    .Select(task => new TaskResponse(task.Id, task.Nome, task.Descricao,
                        task.Data, task.Valor, task.IdUsuario,
                        task.Concluida ? ApiTaskStatus.Completed : ApiTaskStatus.Pending))
                    .ToList()));
    }
}

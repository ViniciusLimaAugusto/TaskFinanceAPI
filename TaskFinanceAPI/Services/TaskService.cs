using Microsoft.EntityFrameworkCore;
using TaskFinanceAPI.Data;
using TaskFinanceAPI.DTOs;
using TaskFinanceAPI.Models;
using ApiTaskStatus = TaskFinanceAPI.DTOs.TaskStatus;

namespace TaskFinanceAPI.Services;

public sealed class TaskService(AppDbContext database)
{
    public IQueryable<TaskResponse> Query(int? id = null)
    {
        var query = database.Tarefas.AsNoTracking();
        if (id is int taskId)
        {
            query = query.Where(task => task.Id == taskId);
        }

        return query.OrderBy(task => task.Id)
            .Select(task => new TaskResponse(task.Id, task.Nome, task.Descricao, task.Data,
                task.Valor, task.IdUsuario,
                task.Concluida ? ApiTaskStatus.Completed : ApiTaskStatus.Pending));
    }

    public async Task<TaskResponse?> CreateAsync(TaskRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnerExistsAsync(request.UserId, cancellationToken))
        {
            return null;
        }

        var task = new Tarefa { Nome = request.Name, Descricao = request.Description };
        ApplyRequest(task, request);
        database.Tarefas.Add(task);
        await database.SaveChangesAsync(cancellationToken);
        return new TaskResponse(task.Id, task.Nome, task.Descricao, task.Data,
            task.Valor, task.IdUsuario, request.Status);
    }

    public async Task<TaskUpdateResult> UpdateAsync(
        int id, TaskRequest request, CancellationToken cancellationToken)
    {
        var task = await database.Tarefas.FindAsync([id], cancellationToken);
        if (task is null)
        {
            return TaskUpdateResult.TaskMissing;
        }

        if (!await OwnerExistsAsync(request.UserId, cancellationToken))
        {
            return TaskUpdateResult.OwnerMissing;
        }

        ApplyRequest(task, request);
        await database.SaveChangesAsync(cancellationToken);
        return TaskUpdateResult.Updated;
    }

    private Task<bool> OwnerExistsAsync(int id, CancellationToken cancellationToken) =>
        database.Usuarios.AnyAsync(user => user.Id == id, cancellationToken);

    private static void ApplyRequest(Tarefa task, TaskRequest request)
    {
        task.Nome = request.Name;
        task.Descricao = request.Description;
        task.Valor = request.Value;
        task.IdUsuario = request.UserId;
        task.Concluida = request.Status == ApiTaskStatus.Completed;
    }
}

public enum TaskUpdateResult
{
    Updated,
    TaskMissing,
    OwnerMissing
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskFinanceAPI.DTOs;
using ApiTaskStatus = TaskFinanceAPI.DTOs.TaskStatus;

namespace TaskFinanceAPI.Tests;

public sealed class ApiTests : IAsyncLifetime
{
    private readonly ApiFactory factory = new();
    private HttpClient client = null!;

    public async Task InitializeAsync()
    {
        await factory.PrepareDatabaseAsync();
        client = factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        client?.Dispose();
        await factory.DisposeAsync();
    }

    [Fact]
    public async Task UsersCanBeCreatedReadUpdatedAndDeleted()
    {
        var user = await CreateUserAsync();
        Assert.Empty(user.Tasks);

        var users = await client.GetFromJsonAsync<UserResponse[]>("/api/Usuarios");
        Assert.Equal(user.Id, Assert.Single(users!).Id);

        var url = $"/api/Usuarios/{user.Id}";
        using var update = await client.PutAsJsonAsync(url, new UserRequest("Alex Morgan", 74.5));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        var updated = await client.GetFromJsonAsync<UserResponse>(url);
        Assert.Equal("Alex Morgan", updated!.Name);
        Assert.Equal(74.5, updated.Balance);

        using var firstDelete = await client.DeleteAsync(url);
        using var secondDelete = await client.DeleteAsync(url);
        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondDelete.StatusCode);
        using var missing = await client.GetAsync(url);
        await AssertProblemAsync(missing, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TasksCanBeReadUpdatedReassignedAndDeleted()
    {
        var firstOwner = await CreateUserAsync();
        var nextOwner = await CreateUserAsync();
        var task = await CreateTaskAsync(firstOwner.Id);
        var url = $"/api/Tarefas/{task.Id}";
        var initial = await client.GetFromJsonAsync<TaskResponse>(url);
        Assert.Equal(ApiTaskStatus.Pending, initial!.Status);
        Assert.Equal(firstOwner.Id, initial.UserId);

        using var update = await client.PutAsJsonAsync(url,
            new TaskRequest("Review invoice", "Verify the example invoice", 12.50, nextOwner.Id, ApiTaskStatus.Completed));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var allTasks = await client.GetFromJsonAsync<TaskResponse[]>("/api/Tarefas");
        var updated = Assert.Single(allTasks!);
        Assert.Equal("Review invoice", updated.Name);
        Assert.Equal(12.50, updated.Value);
        Assert.Equal(ApiTaskStatus.Completed, updated.Status);
        Assert.Equal(nextOwner.Id, updated.UserId);
        Assert.Equal(initial.CreatedAt, updated.CreatedAt);

        var originalOwner = await client.GetFromJsonAsync<UserResponse>($"/api/Usuarios/{firstOwner.Id}");
        var newOwner = await client.GetFromJsonAsync<UserResponse>($"/api/Usuarios/{nextOwner.Id}");
        Assert.Empty(originalOwner!.Tasks);
        Assert.Equal(updated, Assert.Single(newOwner!.Tasks));

        using var deleted = await client.DeleteAsync(url);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var missing = await client.GetAsync(url);
        await AssertProblemAsync(missing, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletingAUserCascadesToTheirTasks()
    {
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(user.Id);
        using var deleted = await client.DeleteAsync($"/api/Usuarios/{user.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var missing = await client.GetAsync($"/api/Tarefas/{task.Id}");
        await AssertProblemAsync(missing, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvalidRequestsReturnValidationProblemDetails()
    {
        using var invalidUser = await client.PostAsJsonAsync("/api/Usuarios", new UserRequest(" ", 0));
        var userProblem = await ReadProblemAsync<ValidationProblemDetails>(invalidUser, HttpStatusCode.BadRequest);
        Assert.Single(userProblem.Errors);

        using var invalidTask = await client.PostAsJsonAsync("/api/Tarefas",
            new TaskRequest(new string('x', 101), new string('x', 201), -1, 0, (ApiTaskStatus)99));
        var taskProblem = await ReadProblemAsync<ValidationProblemDetails>(invalidTask, HttpStatusCode.BadRequest);
        Assert.Equal(5, taskProblem.Errors.Count);
    }

    [Fact]
    public async Task MissingResourcesAndOwnersReturnNotFound()
    {
        var owner = await CreateUserAsync();
        var task = await CreateTaskAsync(owner.Id);
        var request = new TaskRequest("Budget", "Review budget", 0, int.MaxValue, ApiTaskStatus.Pending);
        using var invalidCreate = await client.PostAsJsonAsync("/api/Tarefas", request);
        using var invalidUpdate = await client.PutAsJsonAsync($"/api/Tarefas/{task.Id}", request);
        using var missingTask = await client.PutAsJsonAsync($"/api/Tarefas/{int.MaxValue}", request);
        using var missingUser = await client.PutAsJsonAsync($"/api/Usuarios/{int.MaxValue}", new UserRequest("Alex", 0));
        await AssertProblemAsync(invalidCreate, HttpStatusCode.NotFound);
        await AssertProblemAsync(invalidUpdate, HttpStatusCode.NotFound);
        await AssertProblemAsync(missingTask, HttpStatusCode.NotFound);
        await AssertProblemAsync(missingUser, HttpStatusCode.NotFound);

        var unchanged = await client.GetFromJsonAsync<TaskResponse>($"/api/Tarefas/{task.Id}");
        Assert.Equal(owner.Id, unchanged!.UserId);
    }

    [Fact]
    public async Task OpenApiAndLivenessAreAvailable()
    {
        using var specification = await client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json");
        var paths = specification!.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/Usuarios", out _));
        Assert.True(paths.TryGetProperty("/api/Tarefas", out _));
        using var health = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    private async Task<UserResponse> CreateUserAsync()
    {
        using var response = await client.PostAsJsonAsync("/api/Usuarios", new UserRequest("Alex Taylor", 50));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.EndsWith($"/api/Usuarios/{user!.Id}", response.Headers.Location!.ToString());
        return user;
    }

    private async Task<TaskResponse> CreateTaskAsync(int userId)
    {
        using var response = await client.PostAsJsonAsync("/api/Tarefas",
            new TaskRequest("Budget", "Review budget", 0, userId, ApiTaskStatus.Pending));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.EndsWith($"/api/Tarefas/{task!.Id}", response.Headers.Location!.ToString());
        return task;
    }

    private static Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status) =>
        ReadProblemAsync<ProblemDetails>(response, status);

    private static async Task<T> ReadProblemAsync<T>(HttpResponseMessage response, HttpStatusCode status)
        where T : ProblemDetails
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<T>();
        Assert.NotNull(problem);
        Assert.Equal((int)status, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        return problem;
    }
}

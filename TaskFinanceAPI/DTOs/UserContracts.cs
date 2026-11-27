using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskFinanceAPI.DTOs;

public sealed record UserRequest(
    [property: JsonPropertyName("nome")] [Required, StringLength(100)] string Name,
    [property: JsonPropertyName("saldo")] [Range(-double.MaxValue, double.MaxValue)] double Balance);

public sealed record UserResponse(
    int Id,
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("saldo")] double Balance,
    [property: JsonPropertyName("tarefas")] IReadOnlyList<TaskResponse> Tasks);

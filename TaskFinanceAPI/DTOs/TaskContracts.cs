using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskFinanceAPI.DTOs;

public enum TaskStatus
{
    Pending = 0,
    Completed = 1
}

public sealed record TaskRequest(
    [property: JsonPropertyName("nome")] [Required, StringLength(100)] string Name,
    [property: JsonPropertyName("descricao")] [Required, StringLength(200)] string Description,
    [property: JsonPropertyName("valor")] [Range(0, double.MaxValue)] double Value,
    [property: JsonPropertyName("idUsuario")] [Range(1, int.MaxValue)] int UserId,
    [EnumDataType(typeof(TaskStatus))] TaskStatus Status);

public sealed record TaskResponse(
    int Id,
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("descricao")] string Description,
    [property: JsonPropertyName("data")] DateTime CreatedAt,
    [property: JsonPropertyName("valor")] double Value,
    [property: JsonPropertyName("idUsuario")] int UserId,
    TaskStatus Status);

using System.ComponentModel.DataAnnotations;

namespace TaskFinanceAPI.Models;

public class Tarefa
{
    public int Id { get; set; }

    [MaxLength(100)]
    public required string Nome { get; set; }

    [MaxLength(200)]
    public required string Descricao { get; set; }

    public DateTime Data { get; set; } = DateTime.UtcNow;
    public double Valor { get; set; }
    public bool Concluida { get; set; }
    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = null!;
}

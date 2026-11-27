using System.ComponentModel.DataAnnotations;

namespace TaskFinanceAPI.Models;

public class Usuario
{
    public int Id { get; set; }

    [MaxLength(100)]
    public required string Nome { get; set; }

    public double Saldo { get; set; }
    public ICollection<Tarefa> Tarefas { get; } = new List<Tarefa>();
}

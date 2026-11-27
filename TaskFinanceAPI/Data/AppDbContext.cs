using Microsoft.EntityFrameworkCore;
using TaskFinanceAPI.Models;

namespace TaskFinanceAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tarefa>()
            .HasOne(task => task.Usuario)
            .WithMany(user => user.Tarefas)
            .HasForeignKey(task => task.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

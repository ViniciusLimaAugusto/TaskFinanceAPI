using Microsoft.EntityFrameworkCore;
using TaskFinanceAPI.Data;
using TaskFinanceAPI.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<PersistenceExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<TaskService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapOpenApi();
app.MapHealthChecks("/health/live");
app.MapControllers();
app.Run();

public partial class Program;

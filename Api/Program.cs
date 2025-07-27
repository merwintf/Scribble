using LibraryDemo.Infrastructure.Messaging;
using LibraryDemo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

// MySQL + EF Core
builder.Services.AddDbContext<LibraryDbContext>(opts =>
    opts.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 30)))
);

// Memory cache
builder.Services.AddMemoryCache();

// Wolverine (transactions, outbox, RabbitMQ, event/handler discovery)
builder.Host.UseWolverine(opts => WolverineConfig.Configure(opts));

// API + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

using Microsoft.EntityFrameworkCore;
using PedidosNet.Data;
using PedidosNet.Domain.Factory;
using PedidosNet.Domain.Interfaces;
using PedidosNet.Domain.Strategies;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Corta o ciclo Pedido -> Itens -> ItemPedido -> Pedido -> ...
        // Fix definitivo (DTOs de resposta) entra organicamente mais adiante no curso.
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PedidosNet — Sistema Ingênuo", Version = "v1" });
});

// D.I — Injeção de dependência: registra a implementação concreta PedidoFactory para a interface IPedidoFactory.
builder.Services.AddScoped<IPedidoFactory, PedidoFactory>();
builder.Services.AddScoped<IDescontoStrategy, DescontoVip>();
builder.Services.AddScoped<IDescontoStrategy, DescontoValorPedido>();
builder.Services.AddScoped<IDescontoStrategy, DescontoCupom>();
builder.Services.AddScoped<SeletorDeDesconto>();


var app = builder.Build();

// ⚠️ EnsureCreated + seed fixo — sem migrations. Coerente com o estágio
// "ingênuo" do curso; migrations formais entram quando a Infrastructure
// virar uma camada de verdade na Clean Architecture.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PedidosNet v1"));
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

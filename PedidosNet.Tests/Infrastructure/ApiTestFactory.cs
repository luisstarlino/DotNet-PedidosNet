using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PedidosNet.Data;

namespace PedidosNet.Tests.Infrastructure;

/// <summary>
/// Sobe a API real (Program.cs) em servidor in-memory, porém trocando o
/// provider SQLite que aponta para o arquivo "pedidosnet.db" por uma
/// conexão SQLite em memória, isolada e descartável.
///
/// - Cada instância desta factory cria uma conexão nova (":memory:") mantida
///   aberta pelo seu tempo de vida; ao ser descartada, o banco some.
/// - O schema é criado pelo próprio startup da API (EnsureCreated) e os dados
///   de exemplo são inseridos pelo DbSeeder — exatamente como em produção,
///   mas contra o banco temporário.
/// - O arquivo .db real NUNCA é lido nem alterado: a configuração que o
///   referencia é removida do contêiner de DI antes de o banco ser criado.
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public ApiTestFactory()
    {
        // "DataSource=:memory:" + conexão mantida aberta => o banco vive
        // enquanto a conexão existir. Fechou a conexão, o banco é descartado.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ambiente != Development: desliga o Swagger no pipeline de teste.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove toda a configuração de DbContext herdada do Program.cs,
            // que apontaria para "Data Source=pedidosnet.db" (o banco real).
            RemoveAll(services, typeof(DbContextOptions<AppDbContext>));
            RemoveAll(services, typeof(DbContextOptions));
            RemoveAll(services, typeof(DbConnection));
            RemoveAll(services, typeof(AppDbContext));

            // Reaponta o AppDbContext para a conexão SQLite em memória do teste.
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    private static void RemoveAll(IServiceCollection services, Type serviceType)
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == serviceType).ToList())
            services.Remove(descriptor);
    }

    /// <summary>
    /// Executa uma ação sobre um AppDbContext resolvido num escopo próprio.
    /// Útil para "arrange" (inserir dados extras) e "assert" direto no banco.
    /// </summary>
    public async Task ExecuteDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

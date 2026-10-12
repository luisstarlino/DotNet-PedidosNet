using Microsoft.EntityFrameworkCore;
using PedidosNet.Data;
using PedidosNet.Tests.Infrastructure;
using Shouldly;

namespace PedidosNet.Tests.Infrastructure;

/// <summary>
/// Valida a própria infraestrutura de teste: a fixture sobe a API,
/// cria o schema no SQLite em memória e aplica o DbSeeder.
/// </summary>
public class InfrastructureSmokeTests : IntegrationTestBase
{
    [Fact]
    public async Task Fixture_AoSubirApi_CriaSchemaESemeiaDadosDeExemplo()
    {
        await Factory.ExecuteDbContextAsync(async db =>
        {
            (await db.Clientes.CountAsync()).ShouldBe(4);
            (await db.Produtos.CountAsync()).ShouldBe(6);
            (await db.Pedidos.CountAsync()).ShouldBe(3);
        });
    }

    [Fact]
    public async Task Fixture_CadaTeste_RecebeBancoIsolado()
    {
        // Insere um cliente extra; como o banco é por-teste, não vaza para outros.
        await Factory.ExecuteDbContextAsync(async db =>
        {
            db.Clientes.Add(new PedidosNet.Domain.Models.Cliente { Id = Guid.NewGuid(), Nome = "Temp" });
            await db.SaveChangesAsync();
            (await db.Clientes.CountAsync()).ShouldBe(5);
        });
    }
}

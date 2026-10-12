using System.Net;
using System.Net.Http.Json;
using System.Text;
using PedidosNet.Data;
using PedidosNet.Domain.Models;
using PedidosNet.Tests.Infrastructure;
using Shouldly;

namespace PedidosNet.Tests.Integration;

public class ClientesEndpointsTests : IntegrationTestBase
{
    // ---------- GET /api/clientes ----------
    [Fact]
    public async Task ListarClientes_ComBancoSemeado_RetornaOkComOsQuatroClientes()
    {
        var resposta = await Client.GetAsync("/api/clientes");
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var clientes = await resposta.Content.ReadFromJsonAsync<List<Cliente>>(Json);
        clientes!.Count.ShouldBe(4);
    }

    // ---------- GET /api/clientes/{id} ----------
    [Fact]
    public async Task ObterClientePorId_QuandoExiste_RetornaOkComOCliente()
    {
        var resposta = await Client.GetAsync($"/api/clientes/{DbSeeder.ClienteAnaId}");
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var cliente = await resposta.Content.ReadFromJsonAsync<Cliente>(Json);
        cliente!.Nome.ShouldBe("Ana Ferreira");
        cliente.Vip.ShouldBeTrue();
    }

    [Fact]
    public async Task ObterClientePorId_QuandoNaoExiste_RetornaNotFound()
    {
        var resposta = await Client.GetAsync($"/api/clientes/{Guid.NewGuid()}");
        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ObterClientePorId_ComIdDeTipoInvalido_RetornaNotFound()
    {
        // Restrição de rota {id:guid}: valor não-GUID não casa a rota → 404.
        var resposta = await Client.GetAsync("/api/clientes/nao-e-um-guid");
        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---------- POST /api/clientes ----------
    [Fact]
    public async Task CriarCliente_ComDadosValidos_RetornaOkEPersisteComIdEDataGerados()
    {
        var novo = new Cliente { Nome = "Eduardo Lima", Email = "edu@ex.com", Vip = true };

        var resposta = await Client.PostAsJsonAsync("/api/clientes", novo);
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var id = await resposta.Content.ReadFromJsonAsync<Guid>(Json);
        id.ShouldNotBe(Guid.Empty);

        await Factory.ExecuteDbContextAsync(async db =>
        {
            var persistido = await db.Clientes.FindAsync(id);
            persistido.ShouldNotBeNull();
            persistido!.Nome.ShouldBe("Eduardo Lima");
            persistido.DataCadastro.ShouldNotBe(default); // preenchido pelo controller
        });
    }

    [Fact]
    public async Task CriarCliente_ComCampoDeTipoInvalido_RetornaBadRequest()
    {
        // "vip" deveria ser bool; string não-conversível falha no model binding.
        var json = """{ "nome": "Teste", "vip": "isto-nao-e-bool" }""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var resposta = await Client.PostAsync("/api/clientes", content);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ---------- PUT /api/clientes/{id} ----------
    [Fact]
    public async Task AtualizarCliente_QuandoExiste_RetornaNoContentEAtualizaCampos()
    {
        var alteracao = new Cliente { Nome = "Ana Atualizada", Email = "ana.nova@ex.com", Vip = false, Bloqueado = true };

        var resposta = await Client.PutAsJsonAsync($"/api/clientes/{DbSeeder.ClienteAnaId}", alteracao);
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await Factory.ExecuteDbContextAsync(async db =>
        {
            var atualizado = await db.Clientes.FindAsync(DbSeeder.ClienteAnaId);
            atualizado!.Nome.ShouldBe("Ana Atualizada");
            atualizado.Vip.ShouldBeFalse();
            atualizado.Bloqueado.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task AtualizarCliente_QuandoNaoExiste_RetornaNotFound()
    {
        var resposta = await Client.PutAsJsonAsync($"/api/clientes/{Guid.NewGuid()}", new Cliente { Nome = "X" });
        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---------- DELETE /api/clientes/{id} ----------
    [Fact]
    public async Task RemoverCliente_QuandoExiste_RetornaNoContentERemoveDoBanco()
    {
        var resposta = await Client.DeleteAsync($"/api/clientes/{DbSeeder.ClienteCarlaId}");
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await Factory.ExecuteDbContextAsync(async db =>
            (await db.Clientes.FindAsync(DbSeeder.ClienteCarlaId)).ShouldBeNull());
    }

    [Fact]
    public async Task RemoverCliente_QuandoNaoExiste_RetornaNotFound()
    {
        var resposta = await Client.DeleteAsync($"/api/clientes/{Guid.NewGuid()}");
        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

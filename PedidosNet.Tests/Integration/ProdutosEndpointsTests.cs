using System.Net;
using System.Net.Http.Json;
using System.Text;
using PedidosNet.Data;
using PedidosNet.Domain.Models;
using PedidosNet.Tests.Infrastructure;
using Shouldly;

namespace PedidosNet.Tests.Integration;

public class ProdutosEndpointsTests : IntegrationTestBase
{
    // ---------- GET /api/produtos ----------
    [Fact]
    public async Task ListarProdutos_ComBancoSemeado_RetornaOkComOsSeisProdutos()
    {
        var resposta = await Client.GetAsync("/api/produtos");
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var produtos = await resposta.Content.ReadFromJsonAsync<List<Produto>>(Json);
        produtos!.Count.ShouldBe(6);
    }

    // ---------- GET /api/produtos/{id} ----------
    [Fact]
    public async Task ObterProdutoPorId_QuandoExiste_RetornaOkComOProduto()
    {
        var resposta = await Client.GetAsync($"/api/produtos/{DbSeeder.ProdutoNotebookId}");
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var produto = await resposta.Content.ReadFromJsonAsync<Produto>(Json);
        produto!.Preco.ShouldBe(6500.00m);
    }

    [Fact]
    public async Task ObterProdutoPorId_QuandoNaoExiste_RetornaNotFound()
    {
        var resposta = await Client.GetAsync($"/api/produtos/{Guid.NewGuid()}");
        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ObterProdutoPorId_ComIdDeTipoInvalido_RetornaNotFound()
    {
        var resposta = await Client.GetAsync("/api/produtos/123-abc");
        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---------- POST /api/produtos ----------
    [Fact]
    public async Task CriarProduto_ComDadosValidos_RetornaOkEPersiste()
    {
        var novo = new Produto { Nome = "SSD NVMe", Categoria = "Armazenamento", Preco = 500m, Peso = 0.1m, Estoque = 30 };

        var resposta = await Client.PostAsJsonAsync("/api/produtos", novo);
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var id = await resposta.Content.ReadFromJsonAsync<Guid>(Json);
        await Factory.ExecuteDbContextAsync(async db =>
        {
            var persistido = await db.Produtos.FindAsync(id);
            persistido!.Nome.ShouldBe("SSD NVMe");
            persistido.Peso.ShouldBe(0.1m);
        });
    }

    [Fact]
    public async Task CriarProduto_ComPrecoDeTipoInvalido_RetornaBadRequest()
    {
        var json = """{ "nome": "X", "preco": "caro" }""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var resposta = await Client.PostAsync("/api/produtos", content);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ---------- PUT /api/produtos/{id} ----------
    [Fact]
    public async Task AtualizarProduto_QuandoExiste_RetornaNoContentEAtualizaCampos()
    {
        var alteracao = new Produto { Nome = "Mouse Pro", Categoria = "Periféricos", Preco = 120m, Estoque = 200 };

        var resposta = await Client.PutAsJsonAsync($"/api/produtos/{DbSeeder.ProdutoMouseId}", alteracao);
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await Factory.ExecuteDbContextAsync(async db =>
        {
            var atualizado = await db.Produtos.FindAsync(DbSeeder.ProdutoMouseId);
            atualizado!.Nome.ShouldBe("Mouse Pro");
            atualizado.Preco.ShouldBe(120m);
            atualizado.Estoque.ShouldBe(200);
        });
    }

    [Fact]
    public async Task AtualizarProduto_QuandoNaoExiste_RetornaNotFound()
    {
        var resposta = await Client.PutAsJsonAsync($"/api/produtos/{Guid.NewGuid()}", new Produto { Nome = "X" });
        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---------- DELETE /api/produtos/{id} ----------
    [Fact]
    public async Task RemoverProduto_QuandoExiste_RetornaNoContentERemoveDoBanco()
    {
        var resposta = await Client.DeleteAsync($"/api/produtos/{DbSeeder.ProdutoHeadsetId}");
        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await Factory.ExecuteDbContextAsync(async db =>
            (await db.Produtos.FindAsync(DbSeeder.ProdutoHeadsetId)).ShouldBeNull());
    }

    [Fact]
    public async Task RemoverProduto_QuandoNaoExiste_RetornaNotFound()
    {
        var resposta = await Client.DeleteAsync($"/api/produtos/{Guid.NewGuid()}");
        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

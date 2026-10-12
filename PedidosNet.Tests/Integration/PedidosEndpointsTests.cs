using System.Net;
using System.Net.Http.Json;
using PedidosNet.Data;
using PedidosNet.Domain.Models;
using PedidosNet.DTOs;
using PedidosNet.Tests.Infrastructure;
using Shouldly;

namespace PedidosNet.Tests.Integration;

public class PedidosEndpointsTests : IntegrationTestBase
{
    private static CreatePedidoItemRequest ItemReq(Guid produtoId, int qtd) => new() { ProdutoId = produtoId, Quantidade = qtd };

    // ============================================================
    //  POST /api/pedidos  (pedido comum)
    // ============================================================
    [Fact]
    public async Task CriarPedido_ClienteComumComProdutosValidos_RetornaOkEPersiste()
    {
        var req = new CreatePedidoRequest
        {
            ClienteId = DbSeeder.ClienteCarlaId,
            Itens = { ItemReq(DbSeeder.ProdutoMouseId, 2) } // 90 x 2 = 180
        };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos", req);
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var id = await resposta.Content.ReadFromJsonAsync<Guid>(Json);
        await Factory.ExecuteDbContextAsync(async db =>
        {
            var pedido = await db.Pedidos.FindAsync(id);
            pedido!.Status.ShouldBe("Pendente");
            pedido.Total.ShouldBe(180m); // comum, sem desconto aplicável
        });
    }

    [Fact]
    public async Task CriarPedido_ClienteVip_AplicaDescontoVipEStatusEmAnalise()
    {
        // Percorre todas as camadas: controller -> factory -> seletor -> strategy -> banco.
        var req = new CreatePedidoRequest
        {
            ClienteId = DbSeeder.ClienteAnaId, // Ana é VIP
            Itens = { ItemReq(DbSeeder.ProdutoMouseId, 1) } // 90 -> VIP 10% -> 81
        };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos", req);
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var id = await resposta.Content.ReadFromJsonAsync<Guid>(Json);
        await Factory.ExecuteDbContextAsync(async db =>
        {
            var pedido = await db.Pedidos.FindAsync(id);
            pedido!.Status.ShouldBe("EmAnalise");
            pedido.Total.ShouldBe(81m);
        });
    }

    [Fact]
    public async Task CriarPedido_ClienteInexistente_RetornaBadRequest()
    {
        var req = new CreatePedidoRequest { ClienteId = Guid.NewGuid(), Itens = { ItemReq(DbSeeder.ProdutoMouseId, 1) } };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos", req);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("Cliente não encontrado");
    }

    [Fact]
    public async Task CriarPedido_ProdutoInexistente_RetornaBadRequest()
    {
        var req = new CreatePedidoRequest { ClienteId = DbSeeder.ClienteCarlaId, Itens = { ItemReq(Guid.NewGuid(), 1) } };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos", req);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("não encontrado");
    }

    [Fact]
    public async Task CriarPedido_SemClienteId_RetornaBadRequest()
    {
        // ClienteId ausente desserializa como Guid.Empty => cliente não encontrado.
        var req = new CreatePedidoRequest { Itens = { ItemReq(DbSeeder.ProdutoMouseId, 1) } };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos", req);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ============================================================
    //  POST /api/pedidos/kits  (kit promocional)
    // ============================================================
    [Fact]
    public async Task CriarKit_ComDoisProdutosDistintos_RetornaOkEPersiste()
    {
        var req = new CreatePedidoKitRequest
        {
            ClienteId = DbSeeder.ClienteCarlaId,
            Itens =
            {
                ItemReq(DbSeeder.ProdutoMouseId, 1),
                ItemReq(DbSeeder.ProdutoTecladoId, 1)
            }
        };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos/kits", req);
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var id = await resposta.Content.ReadFromJsonAsync<Guid>(Json);
        await Factory.ExecuteDbContextAsync(async db =>
            (await db.Pedidos.FindAsync(id)).ShouldNotBeNull());
    }

    [Fact]
    public async Task CriarKit_ComCupomAcimaDoMinimo_RetornaOkEGravaCupom()
    {
        var req = new CreatePedidoKitRequest
        {
            ClienteId = DbSeeder.ClienteCarlaId,
            Itens =
            {
                ItemReq(DbSeeder.ProdutoNotebookId, 1), // 6500
                ItemReq(DbSeeder.ProdutoMouseId, 1)     // 90  => 6590 > 300
            },
            CupomDesconto = "PROMO10"
        };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos/kits", req);
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var id = await resposta.Content.ReadFromJsonAsync<Guid>(Json);
        await Factory.ExecuteDbContextAsync(async db =>
            (await db.Pedidos.FindAsync(id))!.CuponDesconto.ShouldBe("PROMO10"));
    }

    [Fact]
    public async Task CriarKit_ClienteInexistente_RetornaBadRequest()
    {
        var req = new CreatePedidoKitRequest { ClienteId = Guid.NewGuid() };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos/kits", req);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("Cliente não encontrado");
    }

    [Fact]
    public async Task CriarKit_ClienteBloqueado_RetornaBadRequest()
    {
        var req = new CreatePedidoKitRequest
        {
            ClienteId = DbSeeder.ClienteDaniloId, // Danilo está bloqueado
            Itens = { ItemReq(DbSeeder.ProdutoMouseId, 1), ItemReq(DbSeeder.ProdutoTecladoId, 1) }
        };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos/kits", req);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("bloqueado");
    }

    [Fact]
    public async Task CriarKit_ProdutoInexistente_RetornaBadRequest()
    {
        var req = new CreatePedidoKitRequest
        {
            ClienteId = DbSeeder.ClienteCarlaId,
            Itens = { ItemReq(Guid.NewGuid(), 1) }
        };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos/kits", req);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("não encontrado");
    }

    [Fact]
    public async Task CriarKit_ComMenosDeDoisProdutosDistintos_RetornaBadRequest()
    {
        var req = new CreatePedidoKitRequest
        {
            ClienteId = DbSeeder.ClienteCarlaId,
            Itens = { ItemReq(DbSeeder.ProdutoMouseId, 3) } // 1 produto distinto
        };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos/kits", req);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("produtos distintos");
    }

    [Fact]
    public async Task CriarKit_CupomComValorAbaixoDoMinimo_RetornaBadRequest()
    {
        var req = new CreatePedidoKitRequest
        {
            ClienteId = DbSeeder.ClienteCarlaId,
            Itens = { ItemReq(DbSeeder.ProdutoMouseId, 1) }, // valor 90 <= 300
            CupomDesconto = "PROMO10"
        };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos/kits", req);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("Cupom");
    }

    [Fact]
    public async Task CriarKit_QuandoPesoUltrapassaCincoKg_RetornaBadRequest()
    {
        // Cria um produto pesado via API e monta um kit que estoura o limite de 5kg.
        var pesado = new Produto { Nome = "Impressora", Categoria = "Escritório", Preco = 800m, Peso = 3m, Estoque = 5 };
        var postProduto = await Client.PostAsJsonAsync("/api/produtos", pesado);
        var pesadoId = await postProduto.Content.ReadFromJsonAsync<Guid>(Json);

        var req = new CreatePedidoKitRequest
        {
            ClienteId = DbSeeder.ClienteCarlaId,
            Itens =
            {
                ItemReq(pesadoId, 2),                 // 3kg x 2 = 6kg > 5kg
                ItemReq(DbSeeder.ProdutoMouseId, 1)
            }
        };

        var resposta = await Client.PostAsJsonAsync("/api/pedidos/kits", req);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadAsStringAsync()).ShouldContain("kg");
    }

    // ============================================================
    //  GET /api/pedidos  e  GET /api/pedidos/{id}
    // ============================================================
    [Fact]
    public async Task ListarPedidos_ComBancoSemeado_RetornaOkComOsTresPedidos()
    {
        var resposta = await Client.GetAsync("/api/pedidos");
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var pedidos = await resposta.Content.ReadFromJsonAsync<List<Pedido>>(Json);
        pedidos!.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ObterPedidoPorId_QuandoExiste_RetornaOkComClienteEItens()
    {
        var resposta = await Client.GetAsync($"/api/pedidos/{DbSeeder.PedidoAnaId}");
        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var pedido = await resposta.Content.ReadFromJsonAsync<Pedido>(Json);
        pedido!.Cliente.ShouldNotBeNull();
        pedido.Itens.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ObterPedidoPorId_QuandoNaoExiste_RetornaNotFound()
    {
        var resposta = await Client.GetAsync($"/api/pedidos/{Guid.NewGuid()}");
        resposta.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

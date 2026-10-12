using PedidosNet.Domain.Factory;
using PedidosNet.Domain.Models;
using Shouldly;

namespace PedidosNet.Tests.Unit;

public class PedidoFactoryTests
{
    private readonly PedidoFactory _factory = new();

    private static Cliente Cliente(bool vip = false, bool corporativo = false, string endereco = "Rua A, 123")
        => new() { Id = Guid.NewGuid(), Nome = "Fulano", Endereco = endereco, Vip = vip, Corporativo = corporativo };

    private static ItemPedido Item(decimal preco, int qtd)
        => new() { Id = Guid.NewGuid(), ProdutoId = Guid.NewGuid(), Preco = preco, Quantidade = qtd };

    [Fact]
    public void Criar_ComItens_CalculaTotalComoSomaDePrecoVezesQuantidade()
    {
        var itens = new[] { Item(100m, 2), Item(50m, 3) }; // 200 + 150 = 350

        var pedido = _factory.Criar(Cliente(), itens);

        pedido.Total.ShouldBe(350m);
    }

    [Fact]
    public void Criar_ClienteComum_DefineStatusPendente()
    {
        var pedido = _factory.Criar(Cliente(), new[] { Item(100m, 1) });

        pedido.Status.ShouldBe("Pendente");
    }

    [Fact]
    public void Criar_ClienteVip_DefineStatusEmAnalise()
    {
        var pedido = _factory.Criar(Cliente(vip: true), new[] { Item(100m, 1) });

        pedido.Status.ShouldBe("EmAnalise");
    }

    [Fact]
    public void Criar_ClienteCorporativoComTotalAcimaDe5000_DefineStatusAguardandoValidacao()
    {
        var pedido = _factory.Criar(Cliente(corporativo: true), new[] { Item(6000m, 1) });

        pedido.Status.ShouldBe("AguardandoValidacao");
    }

    [Fact]
    public void Criar_ClienteCorporativoComTotalIgualA5000_DefineStatusPendente()
    {
        // Regra usa "> 5000"; exatamente 5000 não dispara AguardandoValidacao.
        var pedido = _factory.Criar(Cliente(corporativo: true), new[] { Item(5000m, 1) });

        pedido.Status.ShouldBe("Pendente");
    }

    [Fact]
    public void Criar_ClienteCorporativoEVipComTotalAlto_PrioridadeCorporativoAguardandoValidacao()
    {
        // R1 (corporativo + >5000) é avaliada antes de R2 (vip).
        var pedido = _factory.Criar(Cliente(vip: true, corporativo: true), new[] { Item(9000m, 1) });

        pedido.Status.ShouldBe("AguardandoValidacao");
    }

    [Fact]
    public void Criar_SempreCopiaEnderecoDoClienteParaEnderecoEntrega()
    {
        var pedido = _factory.Criar(Cliente(endereco: "Av. Central, 999"), new[] { Item(10m, 1) });

        pedido.EnderecoEntrega.ShouldBe("Av. Central, 999");
    }

    [Fact]
    public void Criar_SempreVinculaClienteId()
    {
        var cliente = Cliente();

        var pedido = _factory.Criar(cliente, new[] { Item(10m, 1) });

        pedido.ClienteId.ShouldBe(cliente.Id);
        pedido.Cliente.ShouldBeSameAs(cliente);
    }
}

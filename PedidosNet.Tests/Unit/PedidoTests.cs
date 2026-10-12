using PedidosNet.Domain.Interfaces;
using PedidosNet.Domain.Models;
using PedidosNet.Domain.Strategies;
using Shouldly;

namespace PedidosNet.Tests.Unit;

public class PedidoTests
{
    private static ItemPedido Item(decimal preco, int qtd)
        => new() { Preco = preco, Quantidade = qtd };

    [Fact]
    public void CalcularTotal_ComVariosItens_SomaPrecoVezesQuantidade()
    {
        var pedido = new Pedido { Itens = { Item(100m, 2), Item(30m, 5) } }; // 200 + 150

        pedido.CalcularTotal();

        pedido.Total.ShouldBe(350m);
    }

    [Fact]
    public void CalcularTotal_SemItens_ResultaZero()
    {
        var pedido = new Pedido();

        pedido.CalcularTotal();

        pedido.Total.ShouldBe(0m);
    }

    [Fact]
    public void AplicarDesconto_ComUnicaStrategy_SubtraiOValorCalculadoDoTotal()
    {
        var pedido = new Pedido { Total = 1000m };

        pedido.AplicarDesconto(new[] { new DescontoCupom() }); // 10% de 1000 = 100

        pedido.Total.ShouldBe(900m);
    }

    [Fact]
    public void AplicarDesconto_SemStrategies_MantemTotal()
    {
        var pedido = new Pedido { Total = 500m };

        pedido.AplicarDesconto(Array.Empty<IDescontoStrategy>());

        pedido.Total.ShouldBe(500m);
    }
}

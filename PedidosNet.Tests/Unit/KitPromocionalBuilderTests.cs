using NSubstitute;
using PedidosNet.Domain.Interfaces;
using PedidosNet.Domain.Models;
using Shouldly;

namespace PedidosNet.Tests.Unit;

public class KitPromocionalBuilderTests
{
    // O builder depende de IPedidoFactory. Nos testes de regra do builder a
    // fábrica é mockada (NSubstitute) para isolar a lógica do próprio builder.
    private readonly IPedidoFactory _factory = Substitute.For<IPedidoFactory>();

    public KitPromocionalBuilderTests()
    {
        // Por padrão, a fábrica devolve um Pedido "cru" com os itens recebidos.
        _factory.Criar(Arg.Any<Cliente>(), Arg.Any<IReadOnlyCollection<ItemPedido>>())
            .Returns(ci => new Pedido
            {
                Id = Guid.NewGuid(),
                Cliente = ci.Arg<Cliente>(),
                Itens = ci.Arg<IReadOnlyCollection<ItemPedido>>().ToList()
            });
    }

    private static Cliente Cliente() => new() { Id = Guid.NewGuid(), Nome = "Fulano" };

    private static Produto Produto(decimal preco = 100m, decimal peso = 1m)
        => new() { Id = Guid.NewGuid(), Nome = "Produto", Preco = preco, Peso = peso };

    private KitPromocionalBuilder Builder() => new(_factory);

    [Fact]
    public void Build_SemClienteInformado_LancaInvalidOperationException()
    {
        var builder = Builder()
            .AddProduto(Produto(), 1)
            .AddProduto(Produto(), 1);

        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Cliente");
    }

    [Fact]
    public void Build_ComMenosDeDoisProdutosDistintos_LancaInvalidOperationException()
    {
        var builder = Builder().ParaCliente(Cliente()).AddProduto(Produto(), 2);

        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("2 produtos distintos");
    }

    [Fact]
    public void Build_ComDoisProdutosDistintos_RetornaPedidoDaFabrica()
    {
        var pedido = Builder()
            .ParaCliente(Cliente())
            .AddProduto(Produto(), 1)
            .AddProduto(Produto(), 1)
            .Build();

        pedido.ShouldNotBeNull();
        pedido.Itens.Count.ShouldBe(2);
    }

    [Fact]
    public void AddProduto_QuandoPesoUltrapassaCincoKg_LancaInvalidOperationException()
    {
        var builder = Builder().ParaCliente(Cliente());

        // 3kg x 2 = 6kg > 5kg
        Should.Throw<InvalidOperationException>(() => builder.AddProduto(Produto(peso: 3m), 2))
            .Message.ShouldContain("kg");
    }

    [Fact]
    public void AddProduto_QuandoPesoNoLimiteDeCincoKg_NaoLanca()
    {
        var builder = Builder().ParaCliente(Cliente());

        // 2.5kg x 2 = 5kg (== limite, não ultrapassa)
        Should.NotThrow(() => builder.AddProduto(Produto(peso: 2.5m), 2));
    }

    [Fact]
    public void AplicarCupom_ComValorAcumuladoAbaixoOuIgualAoMinimo_LancaInvalidOperationException()
    {
        // Valor acumulado = 300 (== mínimo). Regra exige "> 300".
        var builder = Builder().ParaCliente(Cliente()).AddProduto(Produto(preco: 300m), 1);

        Should.Throw<InvalidOperationException>(() => builder.AplicarCupom("PROMO10"));
    }

    [Fact]
    public void AplicarCupom_ComValorAcumuladoAcimaDoMinimo_AplicaCupomNoPedido()
    {
        var pedido = Builder()
            .ParaCliente(Cliente())
            .AddProduto(Produto(preco: 200m), 1) // 200
            .AddProduto(Produto(preco: 200m), 1) // + 200 = 400 > 300
            .AplicarCupom("PROMO10")
            .Build();

        pedido.CuponDesconto.ShouldBe("PROMO10");
    }

    [Fact]
    public void Build_SemCupom_NaoDefineCuponDesconto()
    {
        var pedido = Builder()
            .ParaCliente(Cliente())
            .AddProduto(Produto(), 1)
            .AddProduto(Produto(), 1)
            .Build();

        pedido.CuponDesconto.ShouldBeNull();
    }
}

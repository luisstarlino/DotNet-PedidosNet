using NSubstitute;
using PedidosNet.Domain.Interfaces;
using PedidosNet.Domain.Models;
using PedidosNet.Domain.Strategies;
using Shouldly;

namespace PedidosNet.Tests.Unit;

public class SeletorDeDescontoTests
{
    private static IDescontoStrategy Strategy(bool aplicavel, bool exclusiva = false)
    {
        var s = Substitute.For<IDescontoStrategy>();
        s.AplicarPara(Arg.Any<Pedido>()).Returns(aplicavel);
        s.Exclusiva.Returns(exclusiva);
        return s;
    }

    private static readonly Pedido QualquerPedido = new();

    [Fact]
    public void Selecionar_SemEstrategiaExclusiva_RetornaTodasAsAplicaveis()
    {
        var aplicavel1 = Strategy(aplicavel: true);
        var aplicavel2 = Strategy(aplicavel: true);
        var naoAplicavel = Strategy(aplicavel: false);
        var seletor = new SeletorDeDesconto(new[] { aplicavel1, naoAplicavel, aplicavel2 });

        var resultado = seletor.Selecionar(QualquerPedido).ToList();

        resultado.ShouldBe(new[] { aplicavel1, aplicavel2 });
    }

    [Fact]
    public void Selecionar_ComEstrategiaExclusivaAplicavel_RetornaApenasAExclusiva()
    {
        var comum = Strategy(aplicavel: true);
        var exclusiva = Strategy(aplicavel: true, exclusiva: true);
        var seletor = new SeletorDeDesconto(new[] { comum, exclusiva });

        var resultado = seletor.Selecionar(QualquerPedido).ToList();

        resultado.ShouldHaveSingleItem().ShouldBeSameAs(exclusiva);
    }

    [Fact]
    public void Selecionar_ExclusivaNaoAplicavel_IgnoraAExclusivaERetornaAsComuns()
    {
        var comum = Strategy(aplicavel: true);
        var exclusivaInaplicavel = Strategy(aplicavel: false, exclusiva: true);
        var seletor = new SeletorDeDesconto(new[] { comum, exclusivaInaplicavel });

        var resultado = seletor.Selecionar(QualquerPedido).ToList();

        resultado.ShouldHaveSingleItem().ShouldBeSameAs(comum);
    }

    [Fact]
    public void Selecionar_NenhumaAplicavel_RetornaColecaoVazia()
    {
        var seletor = new SeletorDeDesconto(new[] { Strategy(aplicavel: false), Strategy(aplicavel: false) });

        seletor.Selecionar(QualquerPedido).ShouldBeEmpty();
    }
}

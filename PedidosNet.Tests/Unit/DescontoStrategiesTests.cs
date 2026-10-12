using PedidosNet.Domain.Interfaces;
using PedidosNet.Domain.Models;
using PedidosNet.Domain.Strategies;
using Shouldly;

namespace PedidosNet.Tests.Unit;

public class DescontoStrategiesTests
{
    private static Pedido Pedido(decimal total, bool vip = false, string? cupom = null, DateTime? data = null)
        => new()
        {
            Total = total,
            CuponDesconto = cupom,
            DataCriacao = data ?? new DateTime(2025, 1, 1),
            Cliente = new Cliente { Vip = vip }
        };

    // --- DescontoVip
    [Fact]
    public void DescontoVip_ClienteVip_AplicaVerdadeiro()
        => new DescontoVip().AplicarPara(Pedido(1000m, vip: true)).ShouldBeTrue();

    [Fact]
    public void DescontoVip_ClienteNaoVip_AplicaFalso()
        => new DescontoVip().AplicarPara(Pedido(1000m, vip: false)).ShouldBeFalse();

    [Fact]
    public void DescontoVip_Calcula10PorCentoDoTotal()
        => new DescontoVip().CalcularDesconto(Pedido(1000m, vip: true)).ShouldBe(100m);

    // --- DescontoValorPedido
    [Fact]
    public void DescontoValorPedido_TotalAcimaDe2000_AplicaVerdadeiro()
        => new DescontoValorPedido().AplicarPara(Pedido(2500m)).ShouldBeTrue();

    [Fact]
    public void DescontoValorPedido_TotalIgualA2000_AplicaFalso()
        => new DescontoValorPedido().AplicarPara(Pedido(2000m)).ShouldBeFalse();

    [Fact]
    public void DescontoValorPedido_Calcula5PorCentoDoTotal()
        => new DescontoValorPedido().CalcularDesconto(Pedido(2500m)).ShouldBe(125m);

    // --- DescontoCupom
    [Fact]
    public void DescontoCupom_ComCupom_AplicaVerdadeiro()
        => new DescontoCupom().AplicarPara(Pedido(500m, cupom: "PROMO")).ShouldBeTrue();

    [Fact]
    public void DescontoCupom_SemCupom_AplicaFalso()
        => new DescontoCupom().AplicarPara(Pedido(500m, cupom: null)).ShouldBeFalse();

    [Fact]
    public void DescontoCupom_Calcula10PorCentoDoTotal()
        => new DescontoCupom().CalcularDesconto(Pedido(500m, cupom: "PROMO")).ShouldBe(50m);

    // --- DescontoSemanaDosPais
    private static DateTime DentroDaPromo => new(DateTime.Now.Year, 8, 5);
    private static DateTime ForaDaPromo => new(DateTime.Now.Year, 1, 15);

    [Fact]
    public void DescontoSemanaDosPais_DentroDoPeriodoEAcimaDoMinimo_AplicaVerdadeiro()
        => new DescontoSemanaDosPais().AplicarPara(Pedido(150m, data: DentroDaPromo)).ShouldBeTrue();

    [Fact]
    public void DescontoSemanaDosPais_ForaDoPeriodo_AplicaFalso()
        => new DescontoSemanaDosPais().AplicarPara(Pedido(150m, data: ForaDaPromo)).ShouldBeFalse();

    [Fact]
    public void DescontoSemanaDosPais_DentroDoPeriodoMasAbaixoDoMinimo_AplicaFalso()
        => new DescontoSemanaDosPais().AplicarPara(Pedido(100m, data: DentroDaPromo)).ShouldBeFalse();

    [Fact]
    public void DescontoSemanaDosPais_Calcula20PorCentoDoTotal()
        => new DescontoSemanaDosPais().CalcularDesconto(Pedido(150m, data: DentroDaPromo)).ShouldBe(30m);

    [Fact]
    public void DescontoSemanaDosPais_EhExclusiva()
        => ((IDescontoStrategy)new DescontoSemanaDosPais()).Exclusiva.ShouldBeTrue();

    [Fact]
    public void DemaisStrategies_NaoSaoExclusivasPorPadrao()
    {
        // Exclusiva é membro default da interface (acessível via IDescontoStrategy).
        ((IDescontoStrategy)new DescontoVip()).Exclusiva.ShouldBeFalse();
        ((IDescontoStrategy)new DescontoValorPedido()).Exclusiva.ShouldBeFalse();
        ((IDescontoStrategy)new DescontoCupom()).Exclusiva.ShouldBeFalse();
    }
}

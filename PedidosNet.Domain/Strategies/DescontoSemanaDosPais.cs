using PedidosNet.Domain.Interfaces;
using PedidosNet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PedidosNet.Domain.Strategies
{
    public class DescontoSemanaDosPais : IDescontoStrategy
    {
        private static readonly DateTime DataInicio = new DateTime(DateTime.Now.Year, 8, 3);
        private static readonly DateTime DataFim = new DateTime(DateTime.Now.Year, 8, 7);
        private const decimal ValorMinimo = 100m;

        public bool AplicarPara(Pedido pedido)
        {
            var dataP = pedido.DataCriacao.Date;
            var estaNaPromocao = dataP >= DataInicio && dataP <= DataFim;
            return estaNaPromocao && pedido.Total > ValorMinimo;
        }
        public decimal CalcularDesconto(Pedido pedido) => pedido.Total * 0.20m;

        public bool Exclusiva => true;
    }
}

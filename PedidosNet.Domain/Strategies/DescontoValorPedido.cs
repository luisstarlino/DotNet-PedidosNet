using PedidosNet.Domain.Interfaces;
using PedidosNet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PedidosNet.Domain.Strategies
{
    public class DescontoValorPedido : IDescontoStrategy
    {
        public bool AplicarPara(Pedido pedido) => pedido.Total > 2000m;
        public decimal CalcularDesconto(Pedido pedido) => pedido.Total * 0.05m;
    }
}

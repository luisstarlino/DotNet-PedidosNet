using PedidosNet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PedidosNet.Domain.Interfaces
{
    public interface IDescontoStrategy
    {
        bool AplicarPara(Pedido pedido);
        decimal CalcularDesconto(Pedido pedido);
        bool Exclusiva => false;
    }
}

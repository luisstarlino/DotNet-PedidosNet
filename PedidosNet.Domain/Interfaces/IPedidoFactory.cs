using PedidosNet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PedidosNet.Domain.Interfaces
{
    public interface IPedidoFactory
    {
        Pedido Criar(Cliente cliente, IReadOnlyCollection<ItemPedido> itens);
    }
}

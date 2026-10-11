using PedidosNet.Domain.Interfaces;
using PedidosNet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PedidosNet.Domain.Factory
{
    public class PedidoFactory : IPedidoFactory
    {
        public Pedido Criar(Cliente cliente, IReadOnlyCollection<ItemPedido> itens)
        {
            var pedido = new Pedido()
            {
                Id = Guid.NewGuid(),
                ClienteId = cliente.Id,
                EnderecoEntrega = cliente.Endereco,
                DataCriacao = DateTime.Now,

                Itens = itens.ToList(),
            };
            pedido.CalcularTotal();
            pedido.Status = DefinirStatusPedido(cliente, pedido.Total);
            return pedido;
        }

        private static string DefinirStatusPedido(Cliente cliente, decimal total)
        {
            // --- R1.: Se o cliente for corporativo e o total do pedido for maior que 5000, o status do pedido deve ser "AguardandoValidacao".
            if (cliente.Corporativo && total > 5000) return "AguardandoValidacao";

            //--- R2.: Se o cliente for Vip, o status do pedido deve ser "EmAnalise".
            if (cliente.Vip) return "EmAnalise";

            return "Pendente";
        } 
    }
}

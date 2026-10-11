using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PedidosNet.Domain.Models
{
    public class Pedido
    {
        public Guid Id { get; set; }

        public Guid ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
        public string? CuponDesconto { get; set; }
        public string EnderecoEntrega { get; set; } = string.Empty;

        // ⚠️ Status como string solta — "magic string",
        // sem enum, sem State.
        // Isso é o gatilho da dor que
        // resolvemos no Módulo 4 (State).
        public string Status { get; set; } = string.Empty;

        public decimal Total { get; set; }
        public DateTime DataCriacao { get; set; }

        public List<ItemPedido> Itens { get; set; } = new();

        public void CalcularTotal()
        {
            Total = Itens.Sum(i => i.Quantidade * i.Preco);
        }

        public void AplicarCupom(string cupom, decimal percentual)
        {
            Total -= Total * percentual;
            CuponDesconto = cupom;
        }
    }
}

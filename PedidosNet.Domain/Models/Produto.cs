using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PedidosNet.Domain.Models
{
    public class Produto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public decimal Peso { get; set; }
        public int Estoque { get; set; }
    }
}

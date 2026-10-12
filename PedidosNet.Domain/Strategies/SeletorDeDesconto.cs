using PedidosNet.Domain.Interfaces;
using PedidosNet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PedidosNet.Domain.Strategies
{
    public class SeletorDeDesconto
    {
        private readonly IReadOnlyList<IDescontoStrategy> _strategiesEmOrdem;
        public SeletorDeDesconto(IEnumerable<IDescontoStrategy> strategies)
        {
            _strategiesEmOrdem = strategies.ToList().AsReadOnly();
        }
        public IEnumerable<IDescontoStrategy> Selecionar(Pedido pedido)
        {
            // _strategiesEmOrdem.Where(s => s.AplicarPara(pedido));
            var aplicaveis = _strategiesEmOrdem.Where(s => s.AplicarPara(pedido)).ToList();
            var exclusiva = aplicaveis.FirstOrDefault(s => s.Exclusiva);
            return exclusiva is not null ? new[] { exclusiva } : aplicaveis;
        }
    }
}

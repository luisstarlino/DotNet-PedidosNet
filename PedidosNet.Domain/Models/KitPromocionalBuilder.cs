using PedidosNet.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PedidosNet.Domain.Models
{
    public class KitPromocionalBuilder
    {
        // --- CONST'S
        private readonly IPedidoFactory _pedidoFactory;
        private const decimal PesoMaximoKit = 5.0m; // Peso máximo permitido para o kit promocional
        private const decimal ValorMinimoParaCupom = 300.0m;
        private const int MinimoProdutosDistintos = 2;
        private const decimal PercentualCupom = 0.10m; // 10% de desconto

        // --- INTERNAL VARIABLES
        private Cliente? _cliente;
        private readonly List<ItemPedido> _itens = new();
        private decimal _pesoAcumulado;
        private decimal _valorAcumulado;
        private string? _cupomDesconto;

        // --- CONSTRUCTOR
        public KitPromocionalBuilder(IPedidoFactory pedidoFactory)
        {
            _pedidoFactory = pedidoFactory;
        }

        public KitPromocionalBuilder ParaCliente(Cliente cliente)
        {
            _cliente = cliente;
            return this;
        }

        public KitPromocionalBuilder AddProduto(Produto produto, int quantidade)
        {
            var pesoItem = produto.Peso * quantidade;
            var pesoResultante = _pesoAcumulado + pesoItem;

            if (pesoResultante > PesoMaximoKit)
                throw new InvalidOperationException($"Adicionar '{produto.Nome}' faz o kit ultrapassar {PesoMaximoKit} kg. (resultaria em {pesoResultante} kg.");

            _itens.Add(new ItemPedido
            {
                Id = Guid.NewGuid(),
                ProdutoId = produto.Id,
                NomeProduto = produto.Nome,
                Preco = produto.Preco,
                Quantidade = quantidade,
                Peso = produto.Peso
            });

            _pesoAcumulado = pesoResultante;
            _valorAcumulado += produto.Preco * quantidade;

            return this;
        }

        public KitPromocionalBuilder AplicarCupom(string cupomDesconto)
        {
            if (_valorAcumulado <= ValorMinimoParaCupom)
                throw new InvalidOperationException($"Cupom só pode ser aplicado quando o valor acumulado ultrapassar {ValorMinimoParaCupom:N2}. Valor atual: R$ {_valorAcumulado:N2}");

            _cupomDesconto = cupomDesconto;
            return this;
        }

        public Pedido Build()
        {
            if (_cliente is null)
                throw new InvalidOperationException("Cliente não foi informado");

            if (_itens.Select(i => i.ProdutoId).Distinct().Count() < MinimoProdutosDistintos)
                throw new InvalidOperationException($"Kit promocional deve conter no mínimo {MinimoProdutosDistintos} produtos distintos.");

            var pedido = _pedidoFactory.Criar(_cliente, _itens);

            if (_cupomDesconto is not null)
                pedido.CuponDesconto = _cupomDesconto;

            return pedido;
        }
    }
}

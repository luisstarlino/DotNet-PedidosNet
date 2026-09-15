using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosNet.Data;
using PedidosNet.Domain.Interfaces;
using PedidosNet.Domain.Models;
using PedidosNet.DTOs;

namespace PedidosNet.Controllers;

[ApiController]
[Route("api/pedidos")]
public class PedidoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPedidoFactory _pedidoFactory;

    public PedidoController(AppDbContext context, IPedidoFactory pedidoFactory)
    {
        _context = context;
        _pedidoFactory = pedidoFactory;
    }
    #region --- CRIAR KIT
    [HttpPost("kit")]
    public async Task<IActionResult> CriarKit(CreatePedidoKitRequest request)
    {
        var cliente = await _context.Clientes.FindAsync(request.ClienteId);
        if (cliente is null) return BadRequest("Cliente não encontrado.");
        if (cliente.Bloqueado) return BadRequest("Cliente bloqueado. Não é possível criar pedidos.");

        var itens = new List<ItemPedido>();

        var produtosAdicionados = new HashSet<Guid>();
        decimal pesoAcumulado = 0;
        decimal valorAcumulado = 0;
        const decimal pesoMaximoKit = 5.0m;

        foreach (var itemRequest in request.Itens)
        {
            var produto = await _context.Produtos.FindAsync(itemRequest.ProdutoId);
            if (produto is null) return BadRequest($"Produto {itemRequest.ProdutoId} não encontrado.");

            // Verifica se o produto já foi adicionado ao kit
            if (produtosAdicionados.Contains(produto.Id)) return BadRequest($"Produto {produto.Nome} já foi adicionado ao kit.");

            // Verifica se o peso acumulado excede o limite
            var pesoItem = produto.Peso * itemRequest.Quantidade;

            if (pesoAcumulado + pesoItem > pesoMaximoKit) return BadRequest($"Peso máximo do kit excedido. Peso atual: {pesoAcumulado + pesoItem} kg, Peso máximo permitido: {pesoMaximoKit} kg.");

            pesoAcumulado+= pesoItem;
            valorAcumulado += produto.Preco * itemRequest.Quantidade;
            produtosAdicionados.Add(produto.Id);

            // Adiciona o produto ao kit
            itens.Add(new ItemPedido
            {
                Id = Guid.NewGuid(),
                ProdutoId = produto.Id,
                NomeProduto = produto.Nome,
                Preco = produto.Preco,
                Quantidade = itemRequest.Quantidade
            });
        }

        if(produtosAdicionados.Count < 2) return BadRequest("Um kit deve conter pelo menos 2 produtos diferentes.");

        if(!string.IsNullOrEmpty(request.CupomDesconto) && valorAcumulado <= 300)
        {
            return BadRequest("Para utilizar o cupom de desconto, o valor total do kit deve ser superior a R$ 300,00.");
        }

        var pedido = _pedidoFactory.Criar(cliente, itens);
        return Ok(pedido.Id);
        

    }
    #endregion


    #region --- CRIAR PEDIDO
    [HttpPost]
    public async Task<IActionResult> Criar(CreatePedidoRequest request)
    {
        var cliente = await _context.Clientes.FindAsync(request.ClienteId);
        if (cliente is null) return BadRequest("Cliente não encontrado.");

        var itens = new List<ItemPedido>();

        foreach (var itemRequest in request.Itens)
        {
            var produto = await _context.Produtos.FindAsync(itemRequest.ProdutoId);
            if (produto is null)
                return BadRequest($"Produto {itemRequest.ProdutoId} não encontrado.");

            itens.Add(new ItemPedido
            {
                Id = Guid.NewGuid(),
                ProdutoId = produto.Id,
                NomeProduto = produto.Nome,
                Preco = produto.Preco,
                Quantidade = itemRequest.Quantidade
            });
        }

        // --- Criando um Pedido
        var pedido = _pedidoFactory.Criar(cliente, itens);

        // ⚠️ Regras de negócio direto no controller — o primeiro sinal amarelo
        // piscando no painel . A partir daqui, cada nova regra
        // (cliente corporativo, campanha, valor mínimo...) só engorda este método.
        if (cliente.Vip) pedido.Total -= pedido.Total * 0.10m;

        if (pedido.Total > 2000) pedido.Total -= pedido.Total * 0.05m;

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        return Ok(pedido.Id);
    }

    #endregion

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var pedidos = await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
            .AsNoTracking()
            .ToListAsync();

        return Ok(pedidos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == id);

        return pedido is null ? NotFound() : Ok(pedido);
    }


}

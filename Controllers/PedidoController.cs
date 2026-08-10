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

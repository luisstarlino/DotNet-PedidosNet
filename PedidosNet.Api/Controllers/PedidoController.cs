using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedidosNet.Data;
using PedidosNet.Domain.Interfaces;
using PedidosNet.Domain.Models;
using PedidosNet.Domain.Strategies;
using PedidosNet.DTOs;

namespace PedidosNet.Controllers;

[ApiController]
[Route("api/pedidos")]
public class PedidoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPedidoFactory _pedidoFactory;
    private readonly SeletorDeDesconto _seletorDescontos;

    public PedidoController(AppDbContext context, IPedidoFactory pedidoFactory, SeletorDeDesconto seletorDescontos)
    {
        _context = context;
        _pedidoFactory = pedidoFactory;
        _seletorDescontos = seletorDescontos;
    }
    #region --- CRIAR KIT
    [HttpPost("kits")]
    public async Task<IActionResult> CriarKit(CreatePedidoKitRequest request)
    {
        var cliente = await _context.Clientes.FindAsync(request.ClienteId);
        if (cliente is null) return BadRequest("Cliente não encontrado.");
        if (cliente.Bloqueado) return BadRequest("Cliente bloqueado. Não é possível criar pedidos.");

        var builder = new KitPromocionalBuilder(_pedidoFactory).ParaCliente(cliente);

        try
        {
            foreach (var itemRequest in request.Itens)
            {
                var produto = await _context.Produtos.FindAsync(itemRequest.ProdutoId);
                if (produto is null) return BadRequest($"Produto {itemRequest.ProdutoId} não encontrado.");

                builder.AddProduto(produto, itemRequest.Quantidade);
            }

            if (!string.IsNullOrWhiteSpace(request.CupomDesconto)) builder.AplicarCupom(request.CupomDesconto);

            var pedido = builder.Build();

            return await FinalizarPedido(pedido);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }


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
        return await FinalizarPedido(pedido);
    }

    private async Task<IActionResult> FinalizarPedido(Pedido pedido)
    {
        var strategies = _seletorDescontos.Selecionar(pedido);
        pedido.AplicarDesconto(strategies);
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

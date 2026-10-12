namespace PedidosNet.DTOs
{
    public class CreatePedidoKitRequest
    {
        public Guid ClienteId { get; set; }
        public List<CreatePedidoItemRequest> Itens { get; set; } = new();
        public string CupomDesconto { get; set; } = string.Empty;
    }
}

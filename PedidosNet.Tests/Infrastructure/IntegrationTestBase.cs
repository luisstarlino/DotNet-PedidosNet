using System.Net.Http;
using System.Text.Json;

namespace PedidosNet.Tests.Infrastructure;

/// <summary>
/// Base para testes de integração. Como o xUnit cria uma nova instância da
/// classe de teste por método, cada teste recebe uma <see cref="ApiTestFactory"/>
/// (e, portanto, um banco SQLite em memória) totalmente novo e isolado.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected ApiTestFactory Factory { get; }
    protected HttpClient Client { get; }

    /// <summary>Opções de JSON equivalentes às do ASP.NET Core (camelCase, case-insensitive).</summary>
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected IntegrationTestBase()
    {
        Factory = new ApiTestFactory();
        Client = Factory.CreateClient();
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}

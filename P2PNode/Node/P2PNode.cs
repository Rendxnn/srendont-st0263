using P2PNode.Services;
using System.Security.Cryptography;
using System.Text;

namespace P2PNode.Node;

public class P2PNode
{
    public readonly int _port;
    public readonly int _id;

    public string _address { get; set; }
    public string? _predecessor { get; set; }
    public int _predecessorId { get; set; }
    public string _successor { get; set; }
    public int _successorId { get; set; }

    private readonly NodeClient _client;
    private readonly NodeServer _server;


    public Dictionary<string, string> _fingerTable;

    public P2PNode(int port)
    {
        _port = port;
        _address = $"localhost:{port}";
        _id = P2PServiceImpl.GenerateId(_address);
        _predecessor = null;
        _predecessorId = -1;
        _successor = _address;
        _successorId = _id;

        _client = new(this);
        _server = new(this);

        _fingerTable = new();

        FileService.CreateNodeFolder(port);
    }

    public async Task Run()
    {
        Task serverTask = _server.StartServer();

        Task clientTask = _client.StartClient();

        Task uploadFileTask = _client.HandleResources();

        await Task.WhenAll(serverTask, clientTask);
    }
     
    public bool IsBetween(int id, int start, int end)
    {
        if (start < end)
        {
            return id > start && id < end;
        }
        else
        {
            return id > start || id < end;
        }
    }
}
using Grpc.Core;
using Grpc.Net.Client;
using P2PNode.Services;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Client = P2PNode.P2PService.P2PServiceClient;

namespace P2PNode.Node;

public class P2PNode
{
    private readonly int _port;
    public readonly int _id;

    //chord properties
    public string _address { get; set; }
    public string? _predecessor { get; set; }
    public int _predecessorId { get; set; }
    public string _successor { get; set; }
    public int _successorId { get; set; }


    public P2PNode(int port)
    {
        _port = port;
        _address = $"localhost:{port}";
        _id = GenerateId(_address);
        _predecessor = null;
        _predecessorId = -1;
        _successor = _address;
        _successorId = _id;
    }

    public async Task Run()
    {
        Task serverTask = StartServer();

        Task clientTask = StartClient();

        Task greetTask = GreetNeighbor();

        await Task.WhenAll(serverTask, clientTask);
    } 

    private int GenerateId(string input)
    {
        using var sha1 = SHA1.Create();

        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        
        return BitConverter.ToInt32(hash, 0) & 0x7FFFFFFF;
    }

    public async Task StartServer()
    {
        var server = new Server
        {
            Services = { P2PService.BindService(new P2PServiceImpl(this)) },
            Ports = { new ServerPort("localhost", _port, ServerCredentials.Insecure) }
        };
        server.Start();

        Console.WriteLine($"P2P Node is listening on port {_port}");
        await Task.Delay(-1); 
    }

    public async Task StartClient()
    {
        while (true)
        {
            Console.WriteLine($"Node created with id: {_id}");


            Console.WriteLine("Join into a existing ring? y/n");
            string? joinResponse = Console.ReadLine();
            if (joinResponse?.ToLower() == "y") 
            {
                Console.WriteLine("Enter a existing node address");
                string? existingNodeAddress = Console.ReadLine();
                
                if (existingNodeAddress == null ) continue;

                await Join(existingNodeAddress);
            }

            else
            {
                _predecessor = null;
                _predecessorId = -1;
                _successor = _address;
                _successorId = _id;
                Console.WriteLine("First node added to the ring");
            }

            while (true)
            { 
                await Stabilize();
                await Task.Delay(5000);
            }

        }
    }

    private async Task Join(string existingNodeAddres)
    {
        try
        {
            using GrpcChannel? channel = GrpcChannel.ForAddress($"http://{existingNodeAddres}");
            Client client = new(channel);

            FindSuccessorReply response = await client.FindSuccessorAsync(new FindSuccessorRequest { Id = _id });
            _successor = response.Address;
            _successorId = response.Id;

            Console.WriteLine($"Node {_id} has been added to the ring. Successor {_successorId}. Predecesor {_predecessorId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error joining the ring: " + ex.ToString());
        }
    }

    public async Task Stabilize()
    {
        try
        {
            using var channel = GrpcChannel.ForAddress($"http://{_successor}");
            Client client = new(channel);

            var response = await client.GetPredecessorAsync(new GetPredecessorRequest());

            if (!string.IsNullOrEmpty(response.Address))
            {
                if (IsBetween(response.Id, _id, _successorId))
                {
                    _successor = response.Address;
                    _successorId = response.Id;
                    Console.WriteLine($"Updating successor to {_successorId} ({_successor})");
                }
            }
            var notifyResponse = await client.NotifyAsync(new NotifyRequest { Address = _address, Id = _id });
        }
        catch (RpcException rpcEx)
        {
            Console.WriteLine($"RPC Error while stabilizing: {rpcEx.Status.Detail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while stabilizing: {ex.Message}");
        }
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

    private async Task GreetNeighbor()
    {
        while (true)
        {
            Console.WriteLine("0. Greet predecessor");
            Console.WriteLine("1. Greet successor");
            int.TryParse(Console.ReadLine(), out int neighbor);

            string neighborName = neighbor == 0 ? "predecessor" : "successor";
            string? neighborAddress = neighbor == 0 ? _predecessor : _successor;
            using var channel = GrpcChannel.ForAddress($"http://{neighborAddress}");
            Client client = new(channel);

            MessageReply response = await client.SendMessageAsync(new MessageRequest { Message = $"Hello {neighborName}!", Sender = _id.ToString() });

            if (response.Success)
            {
                Console.WriteLine($"Succesfully sent to {neighborName}");
            }
            else
            {
                Console.WriteLine("Unsuccesfull");
            }
        }
    }


}
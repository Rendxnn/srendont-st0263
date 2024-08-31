using Grpc.Core;
using Grpc.Net.Client;
using P2PNode.Services;


namespace P2PNode.Node;

public class P2PNode
{
    private readonly int _port;

    public P2PNode(int port)
    {
        _port = port;
    }

    public async Task Run()
    {
        Task serverTask = StartServer();

        Task clientTask = StartClient();

        await Task.WhenAll(serverTask, clientTask);
    } 


    public async Task StartServer()
    {
        var server = new Server
        {
            Services = { P2PService.BindService(new P2PServiceImpl()) },
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
            Console.WriteLine("Enter server address");
            string? serverAddress = Console.ReadLine();

            if (serverAddress == null) continue;

            using var channel = GrpcChannel.ForAddress(serverAddress);
            var client = new P2PService.P2PServiceClient(channel);

            Console.Write("Enter a message to send: ");
            string? message = Console.ReadLine();

            var response = await client.SendMessageAsync(new MessageRequest
            {
                Sender = _port.ToString(), 
                Message = message
            });

            Console.WriteLine($"Message sent successfully: {response.Success}");

            await Task.Delay(5000);
        }
    }
}


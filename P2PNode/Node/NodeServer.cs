using Grpc.Core;
using P2PNode.Services;

namespace P2PNode.Node
{
    public class NodeServer
    {
        public P2PNode _node { get; set; }

        public NodeServer(P2PNode node)
        {
            _node = node;
        }
        public async Task StartServer()
        {
            var server = new Server
            {
                Services = { P2PService.BindService(new P2PServiceImpl(_node)) },
                Ports = { new ServerPort("localhost", _node._port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine($"P2P Node is listening on port {_node._port}");
            await Task.Delay(-1);
        }
    }
}

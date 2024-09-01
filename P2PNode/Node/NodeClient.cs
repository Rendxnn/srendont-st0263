using Grpc.Core;
using Grpc.Net.Client;
using Client = P2PNode.P2PService.P2PServiceClient;

namespace P2PNode.Node
{
    public class NodeClient
    {
        public P2PNode _node { get; set; }

        public NodeClient(P2PNode node)
        {
            _node = node;
        }

        public async Task StartClient()
        {
            while (true)
            {
                Console.WriteLine($"Node created with id: {_node._id}");


                Console.WriteLine("Join into a existing ring? y/n");
                string? joinResponse = Console.ReadLine();
                if (joinResponse?.ToLower() == "y")
                {
                    Console.WriteLine("Enter a existing node address");
                    string? existingNodeAddress = Console.ReadLine();

                    if (existingNodeAddress == null) continue;

                    await Join(existingNodeAddress);
                }

                else
                {
                    _node._predecessor = null;
                    _node._predecessorId = -1;
                    _node._successor = _node._address;
                    _node._successorId = _node._id;
                    Console.WriteLine("First node added to the ring");
                }

                while (true)
                {
                    await Stabilize();
                }

            }
        }

        private async Task Join(string existingNodeAddres)
        {
            try
            {
                using GrpcChannel? channel = GrpcChannel.ForAddress($"http://{existingNodeAddres}");
                Client client = new(channel);

                FindSuccessorReply response = await client.FindSuccessorAsync(new FindSuccessorRequest { Id = _node._id });
                _node._successor = response.Address;
                _node._successorId = response.Id;

                Console.WriteLine($"Node {_node._id} has been added to the ring. Successor {_node._successorId}. Predecesor {_node._predecessorId}");
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
                using var channel = GrpcChannel.ForAddress($"http://{_node._successor}");
                Client client = new(channel);

                var response = await client.GetPredecessorAsync(new GetPredecessorRequest());

                if (!string.IsNullOrEmpty(response.Address))
                {
                    if (IsBetween(response.Id, _node._id, _node._successorId))
                    {
                        _node._successor = response.Address;
                        _node._successorId = response.Id;
                        Console.WriteLine($"Updating successor to {_node._successorId} ({_node._successor})");
                    }
                }
                var notifyResponse = await client.NotifyAsync(new NotifyRequest { Address = _node._address, Id = _node._id });
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

        public async Task GreetNeighbor()
        {
            while (true)
            {
                Console.WriteLine("0. Greet predecessor");
                Console.WriteLine("1. Greet successor");
                int.TryParse(Console.ReadLine(), out int neighbor);

                string neighborName = neighbor == 0 ? "predecessor" : "successor";
                string? neighborAddress = neighbor == 0 ? _node._predecessor : _node._successor;
                using var channel = GrpcChannel.ForAddress($"http://{neighborAddress}");
                Client client = new(channel);

                MessageReply response = await client.SendMessageAsync(new MessageRequest { Message = $"Hello {neighborName}!", Sender = _node._id.ToString() });

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
}

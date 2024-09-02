using Grpc.Core;
using Grpc.Net.Client;
using P2PNode.Extensions;
using P2PNode.Services;
using System.Net.Mail;
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
                    await UpdateRingFingerTable();
                }

                else
                {
                    _node._predecessor = null;
                    _node._predecessorId = -1;
                    _node._successor = _node._address;
                    _node._successorId = _node._id;
                    _node._fingerTable.Add($"{_node._id},-1", _node._address);
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
                using GrpcChannel? existingNodeChannel = GrpcChannel.ForAddress($"http://{existingNodeAddres}");
                Client existingNodeClient = new(existingNodeChannel);

                FindSuccessorReply successorResponse = await existingNodeClient.FindSuccessorAsync(new FindSuccessorRequest { Id = _node._id });

                _node._successor = successorResponse.Address;
                _node._successorId = successorResponse.Id;
                _node._fingerTable = successorResponse.FingerTable.Pairs.ToDictionary(pair => pair.Key, pair => pair.Value);
                _node._fingerTable.AddToFT(_node._id, _node._address);

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

        public async Task UpdateRingFingerTable()
        {
            foreach (KeyValuePair<string, string> pair in _node._fingerTable)
            {
                int pairId = int.Parse(pair.Key.Split(",").ToList()[0]);
                if (pairId == _node._id) continue;
                string pairAddress = pair.Value;
                using GrpcChannel? successorChannel = GrpcChannel.ForAddress($"http://{pairAddress}");
                Client successorClient = new(successorChannel);
                UpdateFingerTableReply updateResponse = await successorClient.UpdateFingerTableAsync(_node._fingerTable.ConvertToFTMessage(_node._id));
            }
        }

        public async Task HandleResources()
        {
            while (true)
            {
                Console.WriteLine("0. Upload Reosurce");
                Console.WriteLine("1. Find Reosurce");

                string? choice = Console.ReadLine();

                if (choice == "0")
                {
                    await UploadResource();
                }
            }
        }

        public async Task UploadResource()
        {
            Console.WriteLine("Resource title: (include extension)");
            string? title = Console.ReadLine();

            Console.WriteLine("Reosurce content: ");
            string? content = Console.ReadLine();


            if (title == null || content == null)
            {
                Console.WriteLine("Both title and content are mandatory");
                return;
            }

            int fileCode = P2PServiceImpl.GenerateId(title);

            string responsibleAddress = FindResponsibleNode(fileCode);

            Console.WriteLine("responsible node" + responsibleAddress);

            if (responsibleAddress == _node._address)
            {
                FileService.SaveFile(_node._port, title, content);
                return;
            }
            using GrpcChannel? responsibleChannel = GrpcChannel.ForAddress($"http://{responsibleAddress}");
            Client resposibleClient = new(responsibleChannel);
            UploadResourceReply uploadResourceReply = await resposibleClient.UploadResourceAsync(new UploadResourceRequest { Content = content, Title = title });

            return;
        }

        private string FindResponsibleNode(int fileHash)
        {
            foreach (var pair in _node._fingerTable)
            {
                string range = pair.Key;
                string nodeAddress = pair.Value;

                int[] rangeValues = range.Split(",").Select(x => int.Parse(x)).ToArray();
                int idMin = rangeValues[0];
                int idMax = rangeValues[1];

                if ((fileHash > idMin && fileHash <= idMax) || (idMax == -1 && fileHash > idMin))
                {
                    return nodeAddress;
                }
            }

            return "";
        }
    }
}

using Grpc.Core;
using Grpc.Net.Client;
using P2PNode.Extensions;
using System.Security.Cryptography;
using System.Text;
using Client = P2PNode.P2PService.P2PServiceClient;

namespace P2PNode.Services
{
    public class P2PServiceImpl : P2PService.P2PServiceBase
    {
        private readonly Node.P2PNode _node;
        public P2PServiceImpl(Node.P2PNode node)
        {
            _node = node;
        }

        public override Task<MessageReply> SendMessage(MessageRequest request, ServerCallContext context)
        {
            Console.WriteLine($"Received message from {request.Sender}: {request.Message}");
            return Task.FromResult(new MessageReply { Success = true });
        }

        public override async Task<FindSuccessorReply> FindSuccessor(FindSuccessorRequest request, ServerCallContext context)
        {
            if (_node.IsBetween(request.Id, _node._predecessorId, _node._id))
            {
                return new FindSuccessorReply { Address = _node._address, Id = _node._id, FingerTable = _node._fingerTable.ConvertToFTMessage(_node._id) };
            }
            else
            {
                using var channel = GrpcChannel.ForAddress($"http://{_node._successor}");
                Client client = new(channel);
                return await client.FindSuccessorAsync(request);
            }
        }

        public override Task<NotifyReply> Notify(NotifyRequest request, ServerCallContext context)
        {
            if (_node._predecessor == null || _node.IsBetween(request.Id, _node._predecessorId, _node._id))
            {
                _node._predecessor = request.Address;
                _node._predecessorId = request.Id;
                Console.WriteLine($"Predecessor updated to {request.Id} ({request.Address})");
            }

            return Task.FromResult(new NotifyReply { Success = true });
        }

        public override Task<GetPredecessorReply> GetPredecessor(GetPredecessorRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetPredecessorReply
            {
                Address = _node._predecessor ?? "",
                Id = _node._predecessorId
            });
        }

        public override Task<FindResourceReply> FindResource(FindResourceRequest request, ServerCallContext context)
        {
            string fileContent = FileService.SearchFile(_node._port, request.FileName);

            return Task.FromResult(new  FindResourceReply { FileName = request.FileName, FileContent = fileContent });
        }

        public override Task<UploadResourceReply> UploadResource(UploadResourceRequest request, ServerCallContext context)
        {
            FileService.SaveFile(_node._port, request.Title, request.Content);

            return Task.FromResult(new UploadResourceReply { Uploaded = true });
        }

        public override Task<UpdateFingerTableReply> UpdateFingerTable(FingerTableMessage request, ServerCallContext context)
        {
            _node._fingerTable = request.Pairs.ToDictionary(pair => pair.Key, pair => pair.Value);

            Console.WriteLine("Finger table has been updated");

            return Task.FromResult(new UpdateFingerTableReply { Updated = true });

        }

        public static int GenerateId(string input)
        {
            using var sha1 = SHA1.Create();

            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));

            return BitConverter.ToInt32(hash, 0) & 0x7FFFFFFF;
        }
    }
}

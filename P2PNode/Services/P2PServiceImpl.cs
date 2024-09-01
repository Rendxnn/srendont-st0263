using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // Implementación de FindSuccessor
        public override async Task<FindSuccessorReply> FindSuccessor(FindSuccessorRequest request, ServerCallContext context)
        {
            if (_node.IsBetween(request.Id, _node._predecessorId, _node._id))
            {
                // Este nodo es el sucesor
                return new FindSuccessorReply { Address = _node._address, Id = _node._id };
            }
            else
            {
                // Reenviar la solicitud al sucesor
                using var channel = GrpcChannel.ForAddress($"http://{_node._successor}");
                var client = new P2PService.P2PServiceClient(channel);
                return await client.FindSuccessorAsync(request);
            }
        }

        public override async Task<NotifyReply> Notify(NotifyRequest request, ServerCallContext context)
        {
            if (_node._predecessor == null || _node.IsBetween(request.Id, _node._predecessorId, _node._id))
            {
                _node._predecessor = request.Address;
                _node._predecessorId = request.Id;
                Console.WriteLine($"Predecessor actualizado a {request.Id} ({request.Address})");
            }

            return new NotifyReply { Success = true };
        }

        public override Task<GetPredecessorReply> GetPredecessor(GetPredecessorRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetPredecessorReply
            {
                Address = _node._predecessor ?? "",
                Id = _node._predecessorId
            });
        }

    }
}

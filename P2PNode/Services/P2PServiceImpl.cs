using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PNode.Services
{
    public class P2PServiceImpl : P2PService.P2PServiceBase
    {
        public override Task<MessageReply> SendMessage(MessageRequest request, ServerCallContext context)
        {
            Console.WriteLine($"Received message from {request.Sender}: {request.Message}");
            return Task.FromResult(new MessageReply { Success = true });
        }
    }
}

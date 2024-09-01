using Node = P2PNode.Node.P2PNode;

class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 1 || !int.TryParse(args[0], out int portNumber))
        {
            Console.WriteLine("Usage: -- <portNumber>");

            return;
        }

        Node node = new(portNumber);
        await node.Run();

    }
}

using ChessServer;
using System.Net;
using System.Threading.Tasks;

namespace ChessConsoleServer {
    internal class Program {

        static ChessMultiServer? multiServer;

        static async Task Main(string[] args) {
            multiServer = new ChessMultiServer();
            multiServer.ServerStart(new IPEndPoint(IPAddress.Any, 0x5DC));

            await multiServer.AwaitClose();
        }
    }
}

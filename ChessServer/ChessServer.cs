using ChessCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChessServer {

    public class ChessMultiServer {
        public EndPoint serverEndpoint;

        public Socket listener;

        public Dictionary<int, ChessServer> instances = new Dictionary<int, ChessServer>();
        

        public void ServerStart(EndPoint serverEndpoint) {
            this.serverEndpoint = serverEndpoint;
            listener = new Socket(serverEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(serverEndpoint);
            listener.Listen(1000);

            ServerListen();
        }

        public async Task AwaitClose() {
            while (listener != null) {
                await Task.Delay(1000);
            }
        }

        public void ServerStop() {
            foreach (ChessServer server in instances.Values) {
                server.ServerStop();
            }
            instances.Clear();
            listener.Close();
            listener = null;
        }

        private async void ServerListen() {
            while (listener != null) {
                OnSocketConnection(await listener.AcceptAsync());
                QueryServerActiveness();
            }
        }

        public void OnSocketConnection(Socket socket) {
            SocketListen(socket);
        }

        private async void SocketListen(Socket socket) {
            byte[] buffer = new byte[4096];
            int recieved = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
            string data = Encoding.UTF8.GetString(buffer, 0, recieved);
            ChessCommand command = ChessCommand.Deserialize(data);
            if (command.type == CommandType.REQUEST_CONNECTION) {
                int iid = ChessCommand.PortFromPassCode(command.code);
                if (instances.ContainsKey(iid)) {
                    // Connecting to existing instance
                    ChessServer server = instances[iid];
                    server.RecieveCommand(command, socket);
                    server.OnSocketConnection(socket);
                } else {
                    // Create a new instance
                    ChessServer server = new ChessServer();
                    server.gameState = new GameState(OptionsLoader.Get("server_pgn"));
                    instances.Add(iid, server);
                    server.passcode = command.code;
                    server.RecieveCommand(command, socket);
                    server.OnSocketConnection(socket);
                    await Task.Delay(10);
                    server.SendPgn(socket);
                }
            }
        }

        public void QueryServerActiveness() {
            List<int> instanceStopQueue = new List<int>();
            foreach (KeyValuePair<int, ChessServer> server in instances) {
                server.Value.TimerUpdate();
                if (!server.Value.IsServerRecentlyActive()) {
                    server.Value.ServerStop();
                    instanceStopQueue.Add(server.Key);
                }
            }
            foreach (int serverID in instanceStopQueue) {
                instances.Remove(serverID);
            }
        }
    }



    public class ChessServer {

        public EndPoint serverEndpoint;

        public Socket listener;


        public List<Socket> clients = new List<Socket>();

        private Socket whiteSocket = null;
        private Socket blackSocket = null;

        public ulong passcode;

        public GameState gameState;

        private Stopwatch sw;

        public bool IsServerRecentlyActive() {
            bool serverCurrentlyActive = IsColourConnected(GameColour.WHITE) || IsColourConnected(GameColour.BLACK);

            if (serverCurrentlyActive) {
                sw = Stopwatch.StartNew();
            } else {
                if (sw.Elapsed.TotalSeconds > double.Parse(OptionsLoader.Get("server_timeout"))) {
                    return false;
                }
            }

            return true;

        }


        public void ServerStart(EndPoint serverEndpoint) {
            this.serverEndpoint = serverEndpoint;
            listener = new Socket(serverEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(serverEndpoint);
            listener.Listen(100);

            ServerListen();
        }

        private async void ServerListen() {
            while (listener != null) {
                OnSocketConnection(await listener.AcceptAsync());
            }
        }

        public void ServerStop() {
            BroadcastCommand(new ChessCommand(CommandType.SERVER_CLOSED, GameColour.NONE));
            if (listener != null) {
                listener.Close();
                listener = null;
            }

            foreach (Socket socket in clients) {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();
            }
            clients.Clear();
        }

        public void OnSocketConnection(Socket newSocket) {
            clients.Add(newSocket);

            SocketListen(newSocket);
        }

        private async void SocketListen(Socket socket) {
            byte[] buffer = new byte[4096];
            while (clients.Contains(socket)) {
                try {
                    int recieved = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    string data = Encoding.UTF8.GetString(buffer, 0, recieved);
                    if (data[0] == '<') {
                        Debug.WriteLine("SERVER: RESP <= " + data.Replace("\n", "\\n"));
                    } else {
                        Debug.WriteLine("SERVER: RECV <= " + data.Replace("\n", "\\n"));
                        RecieveData(data, socket);
                    }
                } catch (SocketException) {
                    clients.Remove(socket);
                } catch (IndexOutOfRangeException) {
                    clients.Remove(socket);
                }
            }
        }


        public ColourRights GetSocketRights(Socket socket) {
            return (socket == whiteSocket ? ColourRights.WHITE : ColourRights.NONE) | (socket == blackSocket ? ColourRights.BLACK : ColourRights.NONE);
        }




        public void RecieveCommand(ChessCommand command, Socket socket) {
            switch (command.type) {
                case CommandType.MOVE:
                     if (gameState.MakeMoveValidated(command.move, GetSocketRights(socket))) {
                        BroadcastCommand(command);
                    }
                    break;
                case CommandType.UNDO:
                    gameState.UnmakeMove(rights: GetSocketRights(socket));
                    BroadcastCommand(command);
                    break;
                case CommandType.SUBMIT:
                    gameState.SubmitMoves(GetSocketRights(socket));
                    BroadcastCommand(command);
                    break;
                case CommandType.DISCONNECT:
                    clients.Remove(socket);
                    if (whiteSocket == socket) whiteSocket = null;
                    if (blackSocket == socket) blackSocket = null;
                    break;
                case CommandType.REQUEST_CONNECTION:
                    if (command.code != passcode) {
                        SendCommand(new ChessCommand(CommandType.PASSWORD_INCORRECT), socket);
                    } else {
                        if (command.colour.isNone()) {
                            // Don't need to do anything special
                        } else if (command.colour.isWhite()) {
                            if (whiteSocket is null || !whiteSocket.Connected) {
                                whiteSocket = socket;
                            } else {
                                SendCommand(new ChessCommand(CommandType.PLAYER_ALREADY_JOINED), socket);
                            }
                        } else if (command.colour.isBlack()) {
                            if (blackSocket is null || !blackSocket.Connected) {
                                blackSocket = socket;
                            } else {
                                SendCommand(new ChessCommand(CommandType.PLAYER_ALREADY_JOINED), socket);
                            }
                        }
                    }
                    break;
            }
        }

        public void RecieveData(string data, Socket socket) {
            string response = "";
            bool shouldSendPgn = false;

            if (data[1] == ':') {
                response = "<|ACK|>";
            } else if (data[1] == '@') {
                // why the fuck is the client sending us pgn
                response = "<|FBD|>";
            } else {
                response = "<|ERR|>";
            }

            Debug.WriteLine("SERVER: RESP => " + response.Replace("\n", "\\n"));
            socket.Send(Encoding.UTF8.GetBytes(response));

            if (data[1] == ':') {
                // Command
                ChessCommand command = ChessCommand.Deserialize(data);
                if (command.type == CommandType.REQUEST_PGN) {
                    shouldSendPgn = true;
                } else {
                    RecieveCommand(command, socket);
                }
            } else if (data[1] == '@') {

            } else {

            }

            if (shouldSendPgn) {
                SendPgn(socket);
            }
        }

        public void SendPgn(Socket socket) {
            string pgn = "";

            lock (gameState) {
                pgn = gameState.GetPgn();
            }

            SendPgn(pgn, socket);
        }


        public bool SendCommand(ChessCommand command, Socket socket) {
            return SendData(command.Serialize(), socket);
        }

        public bool SendPgn(string pgn, Socket socket) {
            return SendData(":@" + pgn + "@;", socket);
        }

        public bool SendTimerInfo(Socket socket) {
            return SendData(":#" + gameState.timer.us_white.ToString() + ":" + gameState.timer.us_black.ToString() + "#;", socket);
        }

        public bool SendData(string data, Socket socket) {
            Debug.WriteLine("SERVER: SEND => " + data.Replace("\n", "\\n"));
            socket.Send(Encoding.UTF8.GetBytes(data));

            return true;
        }



        public void BroadcastCommand(ChessCommand command) {
            // Broadcast to all clients
            foreach (Socket socket in clients) {
                SendCommand(command, socket);
            }
        }

        public void BroadcastTimerInfo() {
            foreach (Socket socket in clients) {
                SendTimerInfo(socket);
            }
        }



        public bool IsColourConnected(GameColour colour) {
            switch (colour) {
                case GameColour.NONE:
                default:
                    return false;
                case GameColour.WHITE:
                    return whiteSocket != null && whiteSocket.Connected;
                case GameColour.BLACK:
                    return blackSocket != null && blackSocket.Connected;
            }
        }

        Stopwatch deltaTimeMeasurer = new Stopwatch();
        public void TimerUpdate() {
            if (gameState.timer != null) {
                gameState.timer.Tick((long)(deltaTimeMeasurer.Elapsed.TotalMilliseconds * 1000));
            }
        }

    }
}

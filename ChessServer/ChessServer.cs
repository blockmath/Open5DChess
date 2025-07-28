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
using System.Threading.Tasks;

namespace ChessServer {
    public class ChessServer {

        public EndPoint serverEndpoint;

        public Socket listener;


        public List<Socket> clients = new List<Socket>();

        private Socket whiteSocket;
        private Socket blackSocket;


        public GameState gameState;

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
            listener.Close();
            listener = null;

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
            byte[] buffer = new byte[65536];
            while (clients.Contains(socket)) {
                try {
                    int recieved = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    string data = Encoding.UTF8.GetString(buffer, 0, recieved);
                    Debug.WriteLine("RECV <= " + data);
                    RecieveData(data, socket);
                } catch (SocketException) {
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
                    if (command.colour.isNone()) {
                        // Don't need to do anything special
                    } else if (command.colour.isWhite()) {
                        if (whiteSocket is null) {
                            whiteSocket = socket;
                        }
                    } else if (command.colour.isBlack()) {
                        if (blackSocket is null) {
                            blackSocket = socket;
                        }
                    }
                    break;
            }
        }

        public void RecieveData(string data, Socket socket) {
            string response = "";
            bool shouldSendPgn = false;
            if (data[1] == ':') {
                // Command
                ChessCommand command = ChessCommand.Deserialize(data);
                if (command.type == CommandType.REQUEST_PGN) {
                    shouldSendPgn = true;
                } else {
                    RecieveCommand(command, socket);
                }
                response = "<|ACK|>";
            } else if (data[1] == '@') {
                // why the fuck is the client sending us pgn
                response = "<|WTF|>";
            } else {
                response = "<|ERR|>";
            }

            Debug.WriteLine("RESP => " + response);
            socket.Send(Encoding.UTF8.GetBytes(response));

            if (shouldSendPgn) {
                string pgn = "";

                lock (gameState) {
                    pgn = gameState.GetPgn();
                }

                SendPgn(pgn, socket);
            }
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
            Debug.WriteLine("SEND => " + data);
            socket.Send(Encoding.UTF8.GetBytes(data));

            byte[] buffer = new byte[1024];
            int recieved = socket.Receive(buffer);
            string response = Encoding.UTF8.GetString(buffer, 0, recieved);
            Debug.WriteLine("RESP <= " + response);
            return (response == "<|ACK|>");
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


    }
}

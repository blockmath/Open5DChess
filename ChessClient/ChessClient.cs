using ChessCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace ChessClient {
    public class ChessClient {

        public EndPoint serverEndpoint;

        public Socket socket;

        public GameState personalState = new GameState();

        public ulong passcode;

        public string DisconnectReason = "server_disconnected";

        public async Task<bool> Connect(EndPoint serverEndpoint, GameColour asColour) {
            this.serverEndpoint = serverEndpoint;
            socket = new Socket(serverEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try {
                await socket.ConnectAsync(serverEndpoint);
            } catch (SocketException) {
                return false;
            }


            SendCommand(new ChessCommand(CommandType.REQUEST_CONNECTION, asColour, passcode));

            SendCommand(new ChessCommand(CommandType.REQUEST_PGN));

            ClientListen();

            return true;
        }

        public void Disconnect() {
            SendCommand(new ChessCommand(CommandType.DISCONNECT));

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket.Dispose();
            socket = null;
        }

        private async void ClientListen() {
            byte[] buffer = new byte[65536];
            while (socket != null) {
                int recieved;
                try {
                    recieved = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                } catch (SocketException) {
                    socket = null;
                    break;
                }
                string data = Encoding.UTF8.GetString(buffer, 0, recieved);
                if (data[0] == '<') {
                    // Response
                    Debug.WriteLine("CLIENT: RESP <= " + data.Replace("\n", "\\n"));
                } else {
                    Debug.WriteLine("CLIENT: RECV <= " + data.Replace("\n", "\\n"));
                    RecieveData(data);
                }
            }
        }

        public bool SendCommand(ChessCommand command) {
            JustSentCommand = command.type;
            return SendData(command.Serialize());
        }

        public bool SendData(string data) {
            Debug.WriteLine("CLIENT: SEND => " + data.Replace("\n", "\\n"));
            socket.Send(Encoding.UTF8.GetBytes(data));

            return true;
        }


        public void RecieveData(string data) {
            string response = "";
            if (data[1] == ':') {
                // Command
                RecieveCommand(ChessCommand.Deserialize(data));
                response = "<|ACK|>";
            } else if (data[1] == '@') {
                Match match = Regex.Match(data, ":@(.*?)@;", RegexOptions.Singleline);
                RecievePgn(match.Groups[1].Value);
                response = "<|ACK|>";
            } else if (data[1] == '#') {
                Match match = Regex.Match(data, ":#(\\d+):(\\d+)#;");
                RecieveTimerState(long.Parse(match.Groups[1].Value), long.Parse(match.Groups[2].Value));
                response = "<|ACK|>";
            } else {
                response = "<|ERR|>";
            }
            if (socket != null) {
                /*Debug.WriteLine("CLIENT: RESP => " + response.Replace("\n", "\\n"));
                socket.Send(Encoding.UTF8.GetBytes(response));*/
            }
        }

        private CommandType JustSentCommand = CommandType.NONE;

        public void RecieveCommand(ChessCommand command) {
            if (JustSentCommand == command.type && JustSentCommand != CommandType.MOVE) {
                JustSentCommand = CommandType.NONE;
                return;
            }
            switch (command.type) {
                case CommandType.MOVE:
                    personalState.MakeMoveValidated(command.move, ColourRights.BOTH);
                    break;
                case CommandType.UNDO:
                    personalState.UnmakeMove();
                    break;
                case CommandType.SUBMIT:
                    personalState.SubmitMoves();
                    break;
                case CommandType.PLAYERTIMEOUT:
                    if (command.colour.isWhite()) personalState.timer.us_white = -1;
                    if (command.colour.isBlack()) personalState.timer.us_black = -1;
                    break;
                case CommandType.PASSWORD_INCORRECT:
                    DisconnectReason = "server_incorrect_password";
                    goto Disconnected;
                case CommandType.PLAYER_ALREADY_JOINED:
                    DisconnectReason = "server_player_already_joined";
                    goto Disconnected;
                case CommandType.DISCONNECT:
                    DisconnectReason = "server_disconnected";
                Disconnected:
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    socket.Dispose();
                    socket = null;
                    break;
            }
        }

        public void RecieveTimerState(long wtime, long btime) {
            personalState.timer.us_white = wtime;
            personalState.timer.us_black = btime;
        }

        public void RecievePgn(string pgn) {
            personalState.LoadPgnStr(pgn);
        }

    }
}

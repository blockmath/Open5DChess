using ChessCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ChessClient {
    public class ChessClient {

        public EndPoint serverEndpoint;

        public Socket socket;

        public GameState personalState = new GameState();

        public void Connect(EndPoint serverEndpoint, GameColour asColour) {
            this.serverEndpoint = serverEndpoint;
            socket = new Socket(serverEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(serverEndpoint);

            SendCommand(new ChessCommand(CommandType.REQUEST_CONNECTION, asColour));

            SendCommand(new ChessCommand(CommandType.REQUEST_PGN));

            ClientListen();
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
                int recieved = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                RecieveData(Encoding.UTF8.GetString(buffer, 0, recieved));
            }
        }
        



        public bool SendCommand(ChessCommand command) {
            return SendData(command.Serialize());
        }

        public bool SendData(string data) {
            socket.Send(Encoding.UTF8.GetBytes(data));

            byte[] buffer = new byte[65536];
            int recieved = socket.Receive(buffer);
            return (Encoding.UTF8.GetString(buffer, 0, recieved) == "<|ACK|>");
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
            socket.Send(Encoding.UTF8.GetBytes(response));
        }

        public void RecieveCommand(ChessCommand command) {
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

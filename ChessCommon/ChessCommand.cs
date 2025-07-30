using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChessCommon {

    public enum CommandType : int {
        NONE = 0,
        MOVE,
        UNDO,
        SUBMIT,

        PLAYERTIMEOUT,
        SERVER_CLOSED,
        DISCONNECT,
        REQUEST_CONNECTION,
        REQUEST_PGN,

        PASSWORD_INCORRECT,
        PLAYER_ALREADY_JOINED
    }


    public struct ChessCommand {
        public CommandType type;
        public GameColour colour;
        public ulong code;

        public Move move;

        public ChessCommand(CommandType type, GameColour colour = GameColour.NONE, ulong code = 0, Move move = null) {
            this.type = type;
            this.colour = colour;
            this.code = code;
            this.move = move;
        }


        public string Serialize() {
            return "::" + colour.ToString() + ":" + type.ToString() + ":" + code.ToString() + (type == CommandType.MOVE ? (":" + move.ToString()) : "") + ":;";
        }

        public static ChessCommand Deserialize(string str) {
            Match match = Regex.Match(str, "::([^:]*):([^:]*):(-?\\d*)(:[^:]*)?:;");

            ChessCommand command = new ChessCommand();

            GameColour.TryParse(match.Groups[1].Value, out command.colour);

            switch (match.Groups[2].Value) {
                default:
                    command.type = CommandType.NONE;
                    break;
                case "MOVE":
                    command.type = CommandType.MOVE;
                    break;
                case "UNDO":
                    command.type = CommandType.UNDO;
                    break;
                case "SUBMIT":
                    command.type = CommandType.SUBMIT;
                    break;
                case "PLAYERTIMEOUT":
                    command.type = CommandType.PLAYERTIMEOUT;
                    break;
                case "SERVER_CLOSED":
                    command.type = CommandType.SERVER_CLOSED;
                    break;
                case "DISCONNECT":
                    command.type = CommandType.DISCONNECT;
                    break;
                case "REQUEST_CONNECTION":
                    command.type = CommandType.REQUEST_CONNECTION;
                    break;
                case "REQUEST_PGN":
                    command.type = CommandType.REQUEST_PGN;
                    break;
                case "PASSWORD_INCORRECT":
                    command.type = CommandType.PASSWORD_INCORRECT;
                    break;
                case "PLAYER_ALREADY_JOINED":
                    command.type = CommandType.PLAYER_ALREADY_JOINED;
                    break;
            }

            command.code = ulong.Parse(match.Groups[3].Value);

            if (match.Groups[4].Success) {
                command.move = new Move(match.Groups[4].Value);
            } else {
                command.move = null;
            }

            return command;
        }

        public static ulong CodeFromPassCode(string password) {
            return BitConverter.ToUInt64(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password)), 0);
        }

        public static ulong CodeFromPassPort(string password, int port) {
            return (((ulong)port) << 48) | (BitConverter.ToUInt64(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password)), 0) & 0x0000_ffff_ffff_ffff);
        }

        public static int PortFromPassCode(ulong code) {
            return (int)(code >> 48);
        }
    }
}

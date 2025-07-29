using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    }


    public struct ChessCommand {
        public CommandType type;
        public GameColour colour;

        public Move move;

        public ChessCommand(CommandType type, GameColour colour = GameColour.NONE, Move move = null) {
            this.type = type;
            this.colour = colour;
            this.move = move;
        }


        public string Serialize() {
            return "::" + colour.ToString() + ":" + type.ToString() + (type == CommandType.MOVE ? (":" + move.ToString()) : "") + ":;";
        }

        public static ChessCommand Deserialize(string str) {
            Match match = Regex.Match(str, "::([^:]*):([^:]*)(:[^:]*)?:;");

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
            }

            if (match.Groups[3].Success) {
                command.move = new Move(match.Groups[3].Value);
            } else {
                command.move = null;
            }

            return command;
        }
    }
}

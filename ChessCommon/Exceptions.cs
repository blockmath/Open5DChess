
using System;

namespace ChessCommon {

    public class CommandSubmitMoves : Exception {

    }


    public class ChessTimeOutException : Exception {
        public readonly GameColour colour;

        public ChessTimeOutException(GameColour colour) {
            this.colour = colour;
        }
    }

}
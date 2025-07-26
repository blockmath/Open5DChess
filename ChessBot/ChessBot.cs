using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ChessCommon;

namespace ChessBot {

    /// <summary>
    /// 
    /// Any bots should inherit the ChessBot class so they can be wrapped in a BotInterface<T>.
    /// 
    /// Interface:
    /// 
    /// You may implement a constructor, which will be called before the game begins.
    /// You must implement `Think`. (Or don't. Your call.)
    /// You will recieve a copy of the current `GameState`, as well as a timer view.
    /// To make a move, return that `Move`.
    /// To submit moves, throw `CommandSubmitMoves`. (Or just call the helper function provided for you.)
    /// 
    /// Until you submit moves, `Think` will be repeatedly called.
    /// Consider caching decisions so that you don't have to redo an entire search just to make two moves.
    /// Don't worry, your instance will persist for the entire game.
    /// 
    /// </summary>


    public class ChessBot {
        public ChessBot() { }

        public void SubmitMoves() => throw new CommandSubmitMoves();

        public virtual Move Think(GameState gameState, TimerView timer) {

            if (gameState.CanSubmitMoves()) {
                SubmitMoves();
            }

            List<Move> moves = gameState.GetLegalMoves();

            return moves[0];
        }
    }
}

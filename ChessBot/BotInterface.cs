using ChessCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChessBot {
    
    public interface BotInterface {
        void StartThink();

        bool IsThinking();
        Move GetMove();
        ChessBot GetBotInstance();
    }


    public class BotInterface<T> : BotInterface where T : ChessBot, new() {
        private ChessBot bot;
        private GameState gameState;

        private Move chosenMove;
        private Thread botThread;

        public BotInterface(GameState gameState) {
            bot = new T();
            this.gameState = gameState;
        }


        private void Think() {
            GameState copiedState;

            lock (gameState) {
                copiedState = gameState.Clone();
            }

            try {
                Move move = bot.Think(copiedState, gameState.timerView);
                chosenMove = move;
            } catch (CommandSubmitMoves) {
                lock (gameState) {
                    gameState.SubmitMoves();
                }
            }
        }

        public void StartThink() {
            chosenMove = null;

            botThread = new Thread(new ThreadStart(Think));
            botThread.Start();
        }

        public bool IsThinking() => !(botThread is null) && botThread.IsAlive;

        public Move GetMove() => chosenMove;

        public ChessBot GetBotInstance() => bot;
    }
}

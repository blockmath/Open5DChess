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
        void HaltThink();

        bool IsThinking();
        Move GetMove();
        ChessBot GetBotInstance();

        string GetConsoleText();
    }


    public class BotInterface<T> : BotInterface where T : ChessBot, new() {
        private ChessBot bot;
        private ChessClient.ChessClient client;

        private GameColour colour;

        private Move chosenMove;
        private Thread botThread;

        public BotInterface(ChessClient.ChessClient client, GameColour colour) {
            bot = new T();
            this.client = client;
            this.colour = colour;
        }


        private void Think() {

            try {
                Move move = bot.Think(client.personalState, client.personalState.timerView);
                client.personalState.MakeMoveValidated(move, colour.GetRights());
                client.SendCommand(new ChessCommand(CommandType.MOVE, colour, move: move));
            } catch (CommandSubmitMoves) {
                if (client.personalState.CanSubmitMoves()) {
                    client.personalState.SubmitMoves();
                    client.SendCommand(new ChessCommand(CommandType.SUBMIT, colour));
                }
            }
        }

        public void StartThink() {
            chosenMove = null;

            botThread = new Thread(new ThreadStart(Think));
            botThread.Start();
        }

        public void HaltThink() {
            botThread.Abort();
        }

        public bool IsThinking() => !(botThread is null) && botThread.IsAlive;

        public Move GetMove() => chosenMove;

        public ChessBot GetBotInstance() => bot;

        public string GetConsoleText() => bot.ConsoleText;
    }
}

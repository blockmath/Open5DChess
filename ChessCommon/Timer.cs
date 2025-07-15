using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessCommon {
    public class TimerView {
        
        public ulong usWhite => timer.us_white;
        public ulong usBlack => timer.us_black;
        public ulong usIncrement => timer.us_increment;

        public ulong usRemaining => timer.turn.isWhite() ? usWhite : usBlack;
        public ulong usTaken => timer.us_turn_started - usRemaining;

        public TimerView(Timer timer) {
            this.timer = timer;
        }

        private readonly Timer timer;

    }

    public class Timer {
        public ulong us_white { get; private set; }
        public ulong us_black { get; private set; }

        public ulong us_turn_started { get; private set; }

        public ulong us_increment { get; private set; }

        public GameColour turn { get; private set; }

        
        public Timer(ulong us, ulong inc) {
            us_white = us_black = us;
        }

        public void SetTurn(GameColour colour) {
            if (turn.isWhite()) {
                us_white += us_increment;
            } else {
                us_black += us_increment;
            }

            turn = colour;
            us_turn_started = turn.isWhite() ? us_white : us_black;
        }

        public void Tick(ulong us) {
            if (turn.isWhite()) {
                us_white -= us;
            }
            if (turn.isBlack()) {
                us_black -= us;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessCommon {
    public class TimerView {
        
        public long usWhite => timer.us_white;
        public long usBlack => timer.us_black;
        public long usIncrement => timer.us_increment;

        public long usRemaining => timer.turn.isWhite() ? usWhite : usBlack;
        public long usTaken => timer.us_turn_started - usRemaining;

        public TimerView(Timer timer) {
            this.timer = timer;
        }

        public string ToString(GameColour colour) => timer.ToString(colour);

        private readonly Timer timer;

    }

    public class Timer {
        public long us_white { get; private set; }
        public long us_black { get; private set; }

        public long us_turn_started { get; private set; }

        public long us_increment { get; private set; }

        public GameColour turn { get; private set; }

        
        public Timer(long us, long inc) {
            us_white = us_black = us;
            us_increment = inc;
        }

        public void SetTurn(GameColour colour) {
            if (turn.isNone() && colour.isBlack()) {
                // White submitted first move, don't start the timer yet
                return;
            }

            if (turn.isWhite()) {
                us_white += us_increment;
            } else if (turn.isBlack()) {
                us_black += us_increment;
            }

            turn = colour;
            us_turn_started = turn.isWhite() ? us_white : us_black;
        }

        public void Tick(long us) {
            if (turn.isWhite()) {
                us_white -= us;
                if (us_white <= 0) {
                    throw new ChessTimeOutException(GameColour.WHITE);
                }
            }
            if (turn.isBlack()) {
                us_black -= us;
                if (us_black <= 0) {
                    throw new ChessTimeOutException(GameColour.BLACK);
                }
            }
        }

        public void Stop() {
            turn = GameColour.NONE;
        }

        public string ToString(GameColour colour) {
            long us = (colour.isWhite() ? us_white : us_black);

            if (us <= 0) return "0:00.0";

            int total_seconds = (int)(us / 1_000_000);

            int minutes = total_seconds / 60;
            int seconds = total_seconds % 60;

            if (minutes >= 60) {
                int hours = minutes / 60;
                minutes %= 60;

                return hours + ":" + (minutes >= 10 ? "" : "0") + minutes + ":" + (seconds >= 10 ? "" : "0") + seconds;
            } else if (minutes < 1) {
                return "0:" + (seconds >= 10 ? "" : "0") + seconds + "." + (us / 100_000) % 10;
            } else {
                return minutes + ":" + (seconds >= 10 ? "" : "0") + seconds;
            }
        }
    }
}

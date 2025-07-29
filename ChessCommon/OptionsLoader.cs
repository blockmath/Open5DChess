using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessCommon {
    public static class OptionsLoader {
        
        public static Dictionary<string, string> options;

        static OptionsLoader() {
            options = new Dictionary<string, string>();

            string file = File.ReadAllText("options.txt");

            foreach (string line in file.Split('\n')) {
                string key = line.Trim().Split(new char[] { ' ' }, 2)[0];
                string value = line.Trim().Split(new char[] { ' ' }, 2)[1].Trim('"');
                options[key] = value;
            }
        }

        public static string Get(string key) {
            if (options.ContainsKey(key)) return options[key];
            return null;
        }

    }
}

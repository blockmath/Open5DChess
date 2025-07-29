using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessCommon {
    public static class TextLocalizer {

        public static Dictionary<string, string> dialog;

        public static List<string> availableLocales;

        static TextLocalizer() {
            availableLocales = new List<string>(Directory.GetFiles("lang"));

            for (int i = 0; i < availableLocales.Count; ++i) {
                availableLocales[i] = availableLocales[i].Remove(0, 5);
            }

            string optLocale = OptionsLoader.Get("locale");

            if (optLocale != null && optLocale != "") {
                LoadLocale(CultureInfo.CreateSpecificCulture(optLocale));
            } else {
                LoadLocale(CultureInfo.CurrentCulture);
            }

        }

        public static void LoadLocale(string locale) {
            Debug.WriteLine("Loading locale: " + locale);
            Debug.Assert(availableLocales.Contains(locale));

            dialog = new Dictionary<string, string>();

            string file = File.ReadAllText("lang/" + locale);

            foreach (string line in file.Split('\n')) {
                string key = line.Trim().Split(new char[] { ' ' }, 2)[0];
                string value = line.Trim().Split(new char[] { ' ' }, 2)[1].Trim('"');
                dialog[key] = value;
            }
        }

        public static void LoadLocale(CultureInfo culture) {
            Debug.WriteLine("Trying culture: " + culture.IetfLanguageTag.ToLowerInvariant());
            if (availableLocales.Contains(culture.IetfLanguageTag.ToLowerInvariant())) {
                LoadLocale(culture.IetfLanguageTag.ToLowerInvariant());
            } else {
                if (culture.Parent.IetfLanguageTag == "") {
                    Debug.WriteLine("Falling back to en");
                    LoadLocale("en");
                } else {
                    LoadLocale(culture.Parent);
                }
            }
        }

        public static string Get(string key) {
            if (dialog.ContainsKey(key)) return dialog[key];
            return "[" + key + "]";
        }

    }
}

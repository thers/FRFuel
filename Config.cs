using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRFuel
{
    public class Config
    {
        protected Dictionary<string, string> Entries { get; set; }

        public Config(string content)
        {
            Entries = new Dictionary<string, string>();

            if (content == null || content.Length == 0)
            {
                return;
            }

            var splitted = content
                .Split('\n')
                .Where((line) => !line.Trim().StartsWith("#"))
                .Select((line) => line.Trim().Split('='))
                .Where((line) => line.Length == 2);

            foreach (var tuple in splitted)
            {
                Entries.Add(tuple[0], tuple[1]);
            }
        }

        public string Get(string key, string defaultValue = null)
        {
            if (Entries.ContainsKey(key))
            {
                return Entries[key];
            }

            return defaultValue;
        }
    }
}

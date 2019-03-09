using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathRenderingLab.Parsers
{
    public static class PropertyListParser
    {
        /// <summary>
        /// Parse a CSS-like formatted string in a chunk of properties
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ToPropertyList(this string str)
        {
            // Split the property strings into its boundaries
            var propSets = str.Split(';');

            var dict = new Dictionary<string, string>();

            bool ValidString(string s) => !string.IsNullOrWhiteSpace(s);

            // Pick the nonempty property strings and add them into the dictionary
            foreach (var propSet in propSets.Where(ValidString))
            {
                var property = propSet.Split(':');

                // Validate the property and put it in the dictionary
                if (property.Length == 2 && property.All(ValidString))
                    dict[property[0].Trim()] = property[1].Trim();
            }

            return dict;
        }
    }
}

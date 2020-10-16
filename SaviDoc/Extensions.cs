using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Savian.SaviDoc
{
    internal static class Extensions
    {
        internal static bool IsHeaderComment(this Match match)
        {
            bool header = CheckForHeaderElements(match.Value);
            return header;
        }

        private static bool CheckForHeaderElements(string comment)
        {
            var rxHeader = new Regex(@"^[\s\|]*(\s*(Company|Location|Authors?|Support|SAS\sVersion)\s*:\s*)(?<value>[\d\D]+?)$", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
            try
            {
                if (rxHeader.IsMatch(comment))
                    return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to check for header elements.");
            }
            return false;
        }


        /// <summary>
        /// Gets the line number of where a match occurred in a regex
        /// </summary>
        /// <param name="match">The regex match</param>
        /// <param name="s">The original string</param>
        /// <returns>The line number where the match was found</returns>
        public static int GetLineNumber(this Match match, string s)
        {
            var lineNumber = s.Take(match.Index).Count(p => p == '\n') + 1;
            return lineNumber;
        }

        public static string GetValue(this string s)
        {
            if (s == null)
            {
                return string.Empty;
            }
            return s;
        }

        /// <summary>
        /// Determines whether a property is a List
        /// </summary>
        /// <param name="prop">The property to check for List</param>
        /// <returns>A bool indicating whether it is a List type</returns>
        public static bool IsList(this PropertyInfo prop)
        {
            bool islist = prop.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(prop.PropertyType);
            return islist;
        }

        public static void Clear(this System.IO.DirectoryInfo directory)
        {
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
        }
    }
}

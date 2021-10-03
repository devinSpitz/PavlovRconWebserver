using System;

namespace PavlovRconWebserver.Extensions
{
    public static class ExtensionMethods
    {
        public static string CutLineAfter(this string fmt, char tag)
        {
            var index = fmt.LastIndexOf(tag.ToString(), StringComparison.Ordinal); // Character to remove "?"
            if (index > 0)
                fmt = fmt.Substring(0, index); // This will remove all text after character ?
            return fmt;
        }


    }
}
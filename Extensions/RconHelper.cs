using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PavlovRconWebserver.Extensions
{
    public static class RconHelper
    {
        
        
        // https://stackoverflow.com/questions/11454004/calculate-a-md5-hash-from-a-string
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
        
        //https://stackoverflow.com/questions/1715434/is-there-a-way-to-test-if-a-string-is-an-md5-hash
        public static bool IsMD5(string input)
        {
            return !String.IsNullOrEmpty(input) && Regex.IsMatch(input, "^[0-9a-fA-F]{32}$", RegexOptions.Compiled);
        }
        
    }
}
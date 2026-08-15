using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder
{
    public class PasswordGenerator
    {
        private static readonly char[] LowerCaseLetters = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'];
        private static readonly char[] UpperCaseLetters = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'];
        private static readonly char[] Digits = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '0'];
        private static readonly char[] SpecialCharacters = ['`', '~', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '_', '-', '+', '=', '/', '?', '.', '>', ',', '<', '\\', '|', '"'];

        public string GeneratePassword(int length)
        {
            var sb = new StringBuilder();
            var rnd = new Random();
            for (var i = 0; i < length; i++)
            {
                var sequence = i % 4;
                var arr = sequence switch
                {
                    0 => LowerCaseLetters,
                    1 => UpperCaseLetters,
                    2 => Digits,
                    _ => SpecialCharacters,
                };

                var index = rnd.Next(arr.Length);
                sb.Append(arr[index]);
            }
            sb.Shuffle();
            return sb.ToString();
        }
    }
}

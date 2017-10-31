﻿using System;
using System.Security.Cryptography;

namespace Spectero.daemon.Libraries.Core
{
    /*
     * MIT licensed password generator implementation from Membership.GeneratePassword
     * Original source at https://github.com/Microsoft/referencesource/blob/master/System.Web/Security/Membership.cs
     * License: https://github.com/Microsoft/referencesource/blob/master/LICENSE.txt
     */

    public class PasswordUtils
    {
        private static readonly char[] Punctuations = "!@#$%^&*()_-+=[{]};:>|./?".ToCharArray();
        private static readonly char[] StartingChars = { '<', '&' };
        /// <summary>Generates a random password of the specified length.</summary>
        /// <returns>A random password of the specified length.</returns>
        /// <param name="length">The number of characters in the generated password. The length must be between 1 and 128 characters. </param>
        /// <param name="numberOfNonAlphanumericCharacters">The minimum number of non-alphanumeric characters (such as @, #, !, %, &amp;, and so on) in the generated password.</param>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="length" /> is less than 1 or greater than 128 -or-<paramref name="numberOfNonAlphanumericCharacters" /> is less than 0 or greater than <paramref name="length" />. </exception>
        public static string GeneratePassword(int length, int numberOfNonAlphanumericCharacters)
        {
            if (length < 1 || length > 128)
                throw new ArgumentException("password_length_incorrect", nameof(length));
            if (numberOfNonAlphanumericCharacters > length || numberOfNonAlphanumericCharacters < 0)
                throw new ArgumentException("min_required_non_alphanumeric_characters_incorrect", nameof(numberOfNonAlphanumericCharacters));
            string s;
            int matchIndex;
            do
            {
                var data = new byte[length];
                var chArray = new char[length];
                var num1 = 0;
                new RNGCryptoServiceProvider().GetBytes(data);
                for (var index = 0; index < length; ++index)
                {
                    var num2 = (int)data[index] % 87;
                    if (num2 < 10)
                        chArray[index] = (char)(48 + num2);
                    else if (num2 < 36)
                        chArray[index] = (char)(65 + num2 - 10);
                    else if (num2 < 62)
                    {
                        chArray[index] = (char)(97 + num2 - 36);
                    }
                    else
                    {
                        chArray[index] = Punctuations[num2 - 62];
                        ++num1;
                    }
                }
                if (num1 < numberOfNonAlphanumericCharacters)
                {
                    var random = new Random();
                    for (var index1 = 0; index1 < numberOfNonAlphanumericCharacters - num1; ++index1)
                    {
                        int index2;
                        do
                        {
                            index2 = random.Next(0, length);
                        }
                        while (!char.IsLetterOrDigit(chArray[index2]));
                        chArray[index2] = Punctuations[random.Next(0, Punctuations.Length)];
                    }
                }
                s = new string(chArray);
            }
            while (IsDangerousString(s, out matchIndex));
            return s;
        }

        internal static bool IsDangerousString(string s, out int matchIndex)
        {
            //bool inComment = false;
            matchIndex = 0;

            for (var i = 0; ;)
            {

                // Look for the start of one of our patterns 
                var n = s.IndexOfAny(StartingChars, i);

                // If not found, the string is safe
                if (n < 0) return false;

                // If it's the last char, it's safe 
                if (n == s.Length - 1) return false;

                matchIndex = n;

                switch (s[n])
                {
                    case '<':
                        // If the < is followed by a letter or '!', it's unsafe (looks like a tag or HTML comment)
                        if (IsAtoZ(s[n + 1]) || s[n + 1] == '!' || s[n + 1] == '/' || s[n + 1] == '?') return true;
                        break;
                    case '&':
                        // If the & is followed by a #, it's unsafe (e.g. &#83;) 
                        if (s[n + 1] == '#') return true;
                        break;
                }

                // Continue searching
                i = n + 1;
            }
        }

        private static bool IsAtoZ(char c)
        {
            if ((int)c >= 97 && (int)c <= 122)
                return true;
            if ((int)c >= 65)
                return (int)c <= 90;
            return false;
        }
    }
}
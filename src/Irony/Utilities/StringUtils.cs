using System;
using System.Collections.Generic;

namespace Irony
{
    public static class Strings
    {
        public const string AllLatinLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        public const string DecimalDigits = "1234567890";
        public const string OctalDigits = "12345670";
        public const string HexDigits = "1234567890aAbBcCdDeEfF";
        public const string BinaryDigits = "01";

        public static string JoinStrings(string separator, IEnumerable<string> values)
        {
            var list = new StringList();
            list.AddRange(values);
            var arr = new string[list.Count];
            list.CopyTo(arr, 0);
            return string.Join(separator, arr);
        }
    } //class

    public class StringDictionary : Dictionary<string, string>
    {
    }

    public class CharList : List<char>
    {
    }

    // CharHashSet: adding Hash to the name to avoid confusion with System.Runtime.Interoperability.CharSet
    // Adding case sensitivity
    public class CharHashSet : HashSet<char>
    {
        private readonly bool _caseSensitive;

        public CharHashSet(bool caseSensitive = true)
        {
            _caseSensitive = caseSensitive;
        }

        public new void Add(char ch)
        {
            if (_caseSensitive)
                base.Add(ch);
            else
            {
                base.Add(char.ToLowerInvariant(ch));
                base.Add(char.ToUpperInvariant(ch));
            }
        }
    }

    public class TypeList : List<Type>
    {
        public TypeList()
        {
        }

        public TypeList(params Type[] types) : base(types)
        {
        }
    }


    public class StringSet : HashSet<string>
    {
        public StringSet()
        {
        }

        public StringSet(StringComparer comparer) : base(comparer)
        {
        }

        public override string ToString()
        {
            return ToString(" ");
        }

        public void AddRange(params string[] items)
        {
            UnionWith(items);
        }

        public string ToString(string separator)
        {
            return Strings.JoinStrings(separator, this);
        }
    }

    public class StringList : List<string>
    {
        public StringList()
        {
        }

        public StringList(params string[] args)
        {
            AddRange(args);
        }

        public override string ToString()
        {
            return ToString(" ");
        }

        public string ToString(string separator)
        {
            return Strings.JoinStrings(separator, this);
        }

        //Used in sorting suffixes and prefixes; longer strings must come first in sort order
        public static int LongerFirst(string x, string y)
        {
            try
            {
//in case any of them is null
                if (x.Length > y.Length) return -1;
            }
            catch
            {
            }
            if (x == y) return 0;
            return 1;
        }
    } //class
}
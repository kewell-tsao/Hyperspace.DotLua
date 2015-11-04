using System;
using System.Collections.Generic;
#if DNXCORE50
using System.Globalization;
#endif

namespace Irony.Parsing
{

    #region About compound terminals

    /*
     As  it turns out, many terminal types in real-world languages have 3-part structure: prefix-body-suffix
     The body is essentially the terminal "value", while prefix and suffix are used to specify additional 
     information (options), while not  being a part of the terminal itself. 
     For example:
     1. c# numbers, may have 0x prefix for hex representation, and suffixes specifying 
       the exact data type of the literal (f, l, m, etc)
     2. c# string may have "@" prefix which disables escaping inside the string
     3. c# identifiers may have "@" prefix and escape sequences inside - just like strings
     4. Python string may have "u" and "r" prefixes, "r" working the same way as @ in c# strings
     5. VB string literals may have "c" suffix identifying that the literal is a character, not a string
     6. VB number literals and identifiers may have suffixes identifying data type

     So it seems like all these terminals have the format "prefix-body-suffix". 
     The CompoundTerminalBase base class implements base functionality supporting this multi-part structure.
     The IdentifierTerminal, NumberLiteral and StringLiteral classes inherit from this base class. 
     The methods in TerminalFactory static class demonstrate that with this architecture we can define the whole 
     variety of terminals for c#, Python and VB.NET languages. 
  */

    #endregion

    public class EscapeTable : Dictionary<char, char>
    {
    }

    public abstract class CompoundTerminalBase : Terminal
    {
        #region utils: GetDefaultEscapes

        public static EscapeTable GetDefaultEscapes()
        {
            var escapes = new EscapeTable();
            escapes.Add('a', '\u0007');
            escapes.Add('b', '\b');
            escapes.Add('t', '\t');
            escapes.Add('n', '\n');
            escapes.Add('v', '\v');
            escapes.Add('f', '\f');
            escapes.Add('r', '\r');
            escapes.Add('"', '"');
            escapes.Add('\'', '\'');
            escapes.Add('\\', '\\');
            escapes.Add(' ', ' ');
            escapes.Add('\n', '\n'); //this is a special escape of the linebreak itself, 
            // when string ends with "\" char and continues on the next line
            return escapes;
        }

        #endregion

        #region Nested classes

        protected class ScanFlagTable : Dictionary<string, short>
        {
        }

        protected class TypeCodeTable : Dictionary<string, TypeCode[]>
        {
        }

        public class CompoundTokenDetails
        {
            public string Body;
            public string EndSymbol;
            public string Error;
            public string ExponentSymbol; //exponent symbol for Number literal
            public short Flags; //need to be short, because we need to save it in Scanner state for Vs integration
            public bool IsPartial;
            public bool PartialContinues;
            //partial token info, used by VS integration
            public bool PartialOk;
            public string Prefix;
            public string Sign;
            public string StartSymbol; //string start and end symbols
            public byte SubTypeIndex; //used for string literal kind
            public string Suffix;
            public TypeCode[] TypeCodes;
            public object Value;

            public string Text
            {
                get { return Prefix + Body + Suffix; }
            }

            //Flags helper method
            public bool IsSet(short flag)
            {
                return (Flags & flag) != 0;
            }
        }

        #endregion

        #region constructors and initialization

        public CompoundTerminalBase(string name) : this(name, TermFlags.None)
        {
        }

        public CompoundTerminalBase(string name, TermFlags flags) : base(name)
        {
            SetFlag(flags);
            Escapes = GetDefaultEscapes();
        }

        protected void AddPrefixFlag(string prefix, short flags)
        {
            PrefixFlags.Add(prefix, flags);
            Prefixes.Add(prefix);
        }

        public void AddSuffix(string suffix, params TypeCode[] typeCodes)
        {
            SuffixTypeCodes.Add(suffix, typeCodes);
            Suffixes.Add(suffix);
        }

        #endregion

        #region public Properties/Fields

        public char EscapeChar = '\\';
        public EscapeTable Escapes = new EscapeTable();
        //Case sensitivity for prefixes and suffixes
        public bool CaseSensitivePrefixesSuffixes = false;

        #endregion

        #region private fields

        protected readonly ScanFlagTable PrefixFlags = new ScanFlagTable();
        protected readonly TypeCodeTable SuffixTypeCodes = new TypeCodeTable();
        protected StringList Prefixes = new StringList();
        protected StringList Suffixes = new StringList();
        private CharHashSet _prefixesFirsts; //first chars of all prefixes, for fast prefix detection
        private CharHashSet _suffixesFirsts; //first chars of all suffixes, for fast suffix detection

        #endregion

        #region overrides: Init, TryMatch

        public override void Init(GrammarData grammarData)
        {
            base.Init(grammarData);
            //collect all suffixes, prefixes in lists and create sets of first chars for both
            Prefixes.Sort(StringList.LongerFirst);
            Suffixes.Sort(StringList.LongerFirst);

            _prefixesFirsts = new CharHashSet(CaseSensitivePrefixesSuffixes);
            _suffixesFirsts = new CharHashSet(CaseSensitivePrefixesSuffixes);
            foreach (var pfx in Prefixes)
                _prefixesFirsts.Add(pfx[0]);

            foreach (var sfx in Suffixes)
                _suffixesFirsts.Add(sfx[0]);
        } //method

        public override IList<string> GetFirsts()
        {
            return Prefixes;
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source)
        {
            Token token;
            //Try quick parse first, but only if we're not continuing
            if (context.VsLineScanState.Value == 0)
            {
                token = QuickParse(context, source);
                if (token != null) return token;
                source.PreviewPosition = source.Position; //revert the position
            }

            var details = new CompoundTokenDetails();
            InitDetails(context, details);

            if (context.VsLineScanState.Value == 0)
                ReadPrefix(source, details);
            if (!ReadBody(source, details))
                return null;
            if (details.Error != null)
                return context.CreateErrorToken(details.Error);
            if (details.IsPartial)
            {
                details.Value = details.Body;
            }
            else
            {
                ReadSuffix(source, details);

                if (!ConvertValue(details))
                {
                    if (string.IsNullOrEmpty(details.Error))
                        details.Error = Resources.ErrInvNumber;
                    return context.CreateErrorToken(details.Error); // "Failed to convert the value: {0}"
                }
            }
            token = CreateToken(context, source, details);

            if (details.IsPartial)
            {
                //Save terminal state so we can continue
                context.VsLineScanState.TokenSubType = details.SubTypeIndex;
                context.VsLineScanState.TerminalFlags = details.Flags;
                context.VsLineScanState.TerminalIndex = MultilineIndex;
            }
            else
                context.VsLineScanState.Value = 0;
            return token;
        }

        protected virtual Token CreateToken(ParsingContext context, ISourceStream source, CompoundTokenDetails details)
        {
            var token = source.CreateToken(OutputTerminal, details.Value);
            token.Details = details;
            if (details.IsPartial)
                token.Flags |= TokenFlags.IsIncomplete;
            return token;
        }

        protected virtual void InitDetails(ParsingContext context, CompoundTokenDetails details)
        {
            details.PartialOk = (context.Mode == ParseMode.VsLineScan);
            details.PartialContinues = (context.VsLineScanState.Value != 0);
        }

        protected virtual Token QuickParse(ParsingContext context, ISourceStream source)
        {
            return null;
        }

        protected virtual void ReadPrefix(ISourceStream source, CompoundTokenDetails details)
        {
            if (!_prefixesFirsts.Contains(source.PreviewChar))
                return;
#if DNXCORE50
            var stringComparer = CultureInfo.InvariantCulture.CompareInfo.GetStringComparer(CaseSensitivePrefixesSuffixes ? CompareOptions.None: CompareOptions.IgnoreCase);
#else
            var comparisonType = CaseSensitivePrefixesSuffixes
                ? StringComparison.InvariantCulture
                : StringComparison.InvariantCultureIgnoreCase;
#endif
            foreach (var pfx in Prefixes)
            {
                // Prefixes are usually case insensitive, even if language is case-sensitive. So we cannot use source.MatchSymbol here,
                // we need case-specific comparison
#if DNXCORE50
                if (stringComparer.Compare(source.Text.Substring(source.PreviewPosition, pfx.Length), pfx) != 0)
#else
                if (string.Compare(source.Text, source.PreviewPosition, pfx, 0, pfx.Length, comparisonType) != 0)
#endif
                    continue;
                //We found prefix
                details.Prefix = pfx;
                source.PreviewPosition += pfx.Length;
                //Set flag from prefix
                short pfxFlags;
                if (!string.IsNullOrEmpty(details.Prefix) && PrefixFlags.TryGetValue(details.Prefix, out pfxFlags))
                    details.Flags |= pfxFlags;
                return;
            } //foreach
        } //method

        protected virtual bool ReadBody(ISourceStream source, CompoundTokenDetails details)
        {
            return false;
        }

        protected virtual void ReadSuffix(ISourceStream source, CompoundTokenDetails details)
        {
            if (!_suffixesFirsts.Contains(source.PreviewChar)) return;
#if DNXCORE50
            var stringComparer = CultureInfo.InvariantCulture.CompareInfo.GetStringComparer(CaseSensitivePrefixesSuffixes ? CompareOptions.None: CompareOptions.IgnoreCase);
#else
            var comparisonType = CaseSensitivePrefixesSuffixes
                ? StringComparison.InvariantCulture
                : StringComparison.InvariantCultureIgnoreCase;
#endif
            foreach (var sfx in Suffixes)
            {
                //Suffixes are usually case insensitive, even if language is case-sensitive. So we cannot use source.MatchSymbol here,
                // we need case-specific comparison
#if DNXCORE50
                if (stringComparer.Compare(source.Text.Substring(source.PreviewPosition, sfx.Length), sfx) != 0)
#else
                if (string.Compare(source.Text, source.PreviewPosition, sfx, 0, sfx.Length, comparisonType) != 0)
#endif
                    continue;
                //We found suffix
                details.Suffix = sfx;
                source.PreviewPosition += sfx.Length;
                //Set TypeCode from suffix
                TypeCode[] codes;
                if (!string.IsNullOrEmpty(details.Suffix) && SuffixTypeCodes.TryGetValue(details.Suffix, out codes))
                    details.TypeCodes = codes;
                return;
            } //foreach
        } //method

        protected virtual bool ConvertValue(CompoundTokenDetails details)
        {
            details.Value = details.Body;
            return false;
        }

        #endregion
    } //class
} //namespace
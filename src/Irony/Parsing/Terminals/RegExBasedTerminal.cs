using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Irony.Parsing
{
    //Note: this class was not tested at all
    // Based on contributions by CodePlex user sakana280
    // 12.09.2008 - breaking change! added "name" parameter to the constructor
    public class RegexBasedTerminal : Terminal
    {
        public RegexBasedTerminal(string pattern, params string[] prefixes)
            : base("name")
        {
            Pattern = pattern;
            if (prefixes != null)
                Prefixes.AddRange(prefixes);
        }

        public RegexBasedTerminal(string name, string pattern, params string[] prefixes) : base(name)
        {
            Pattern = pattern;
            if (prefixes != null)
                Prefixes.AddRange(prefixes);
        }

        public override void Init(GrammarData grammarData)
        {
            base.Init(grammarData);
            var workPattern = @"\G(" + Pattern + ")";
            var options = (Grammar.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            Expression = new Regex(workPattern, options);
            if (EditorInfo == null)
                EditorInfo = new TokenEditorInfo(TokenType.Unknown, TokenColor.Text, TokenTriggers.None);
        }

        public override IList<string> GetFirsts()
        {
            return Prefixes;
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source)
        {
            var m = Expression.Match(source.Text, source.PreviewPosition);
            if (!m.Success || m.Index != source.PreviewPosition)
                return null;
            source.PreviewPosition += m.Length;
            return source.CreateToken(OutputTerminal);
        }

        #region public properties

        public readonly string Pattern;
        public readonly StringList Prefixes = new StringList();

        public Regex Expression { get; private set; }

        #endregion
    } //class
} //namespace
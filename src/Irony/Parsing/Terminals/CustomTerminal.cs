using System.Collections.Generic;
using System.Diagnostics;

namespace Irony.Parsing
{
    //Terminal based on custom method; allows creating custom match without creating new class derived from Terminal 
    public delegate Token MatchHandler(Terminal terminal, ParsingContext context, ISourceStream source);

    public class CustomTerminal : Terminal
    {
        public readonly StringList Prefixes = new StringList();

        public CustomTerminal(string name, MatchHandler handler, params string[] prefixes) : base(name)
        {
            Handler = handler;
            if (prefixes != null)
                Prefixes.AddRange(prefixes);
            EditorInfo = new TokenEditorInfo(TokenType.Unknown, TokenColor.Text, TokenTriggers.None);
        }

        public MatchHandler Handler { [DebuggerStepThrough] get; }

        public override Token TryMatch(ParsingContext context, ISourceStream source)
        {
            return Handler(this, context, source);
        }

        [DebuggerStepThrough]
        public override IList<string> GetFirsts()
        {
            return Prefixes;
        }
    } //class
}
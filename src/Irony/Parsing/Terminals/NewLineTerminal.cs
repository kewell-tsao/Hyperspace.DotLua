using System.Collections.Generic;
using System.Linq;

namespace Irony.Parsing
{
    //This is a simple NewLine terminal recognizing line terminators for use in grammars for line-based languages like VB
    // instead of more complex alternative of using CodeOutlineFilter. 
    public class NewLineTerminal : Terminal
    {
        public string LineTerminators = "\n\r\v";

        public NewLineTerminal(string name) : base(name, TokenCategory.Outline)
        {
            ErrorAlias = Resources.LabelLineBreak; // "[line break]";
            Flags |= TermFlags.IsPunctuation;
        }

        #region overrides: Init, GetFirsts, TryMatch

        public override void Init(GrammarData grammarData)
        {
            base.Init(grammarData);
            Grammar.UsesNewLine = true; //That will prevent SkipWhitespace method from skipping new-line chars
        }

        public override IList<string> GetFirsts()
        {
            var firsts = new StringList();
            foreach (var t in LineTerminators)
                firsts.Add(t.ToString());
            return firsts;
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source)
        {
            var current = source.PreviewChar;
            if (!LineTerminators.Contains(current)) return null;
            //Treat \r\n as a single terminator
            var doExtraShift = (current == '\r' && source.NextPreviewChar == '\n');
            source.PreviewPosition++; //main shift
            if (doExtraShift)
                source.PreviewPosition++;
            var result = source.CreateToken(OutputTerminal);
            return result;
        }

        #endregion
    } //class
} //namespace
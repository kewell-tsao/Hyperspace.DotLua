using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Irony.Parsing
{
    public class KeyTermTable : Dictionary<string, KeyTerm>
    {
        public KeyTermTable(StringComparer comparer) : base(100, comparer)
        {
        }
    }

    public class KeyTermList : List<KeyTerm>
    {
    }

    //Keyterm is a keyword or a special symbol used in grammar rules, for example: begin, end, while, =, *, etc.
    // So "key" comes from the Keyword. 
    public class KeyTerm : Terminal
    {
        //Normally false, meaning keywords (symbols in grammar consisting of letters) cannot be followed by a letter or digit
        public bool AllowAlphaAfterKeyword = false;

        public KeyTerm(string text, string name) : base(name)
        {
            Text = text;
            ErrorAlias = name;
            Flags |= TermFlags.NoAstNode;
        }

        public string Text { get; }

        [DebuggerStepThrough]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        [DebuggerStepThrough]
        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        #region overrides: TryMatch, Init, GetPrefixes(), ToString() 

        public override void Init(GrammarData grammarData)
        {
            base.Init(grammarData);

            #region comments about keyterms priority

            // Priority - determines the order in which multiple terminals try to match input for a given current char in the input.
            // For a given input char the scanner looks up the collection of terminals that may match this input symbol. It is the order
            // in this collection that is determined by Priority value - the higher the priority, the earlier the terminal gets a chance 
            // to check the input. 
            // Keywords found in grammar by default have lowest priority to allow other terminals (like identifiers)to check the input first.
            // Additionally, longer symbols have higher priority, so symbols like "+=" should have higher priority value than "+" symbol. 
            // As a result, Scanner would first try to match "+=", longer symbol, and if it fails, it will try "+". 
            // Reserved words are the opposite - they have the highest priority

            #endregion

            if (Flags.IsSet(TermFlags.IsReservedWord))
                Priority = TerminalPriority.ReservedWords + Text.Length;
                    //the longer the word, the higher is the priority
            else
                Priority = TerminalPriority.Low + Text.Length;
            //Setup editor info      
            if (EditorInfo != null) return;
            var tknType = TokenType.Identifier;
            if (Flags.IsSet(TermFlags.IsOperator))
                tknType |= TokenType.Operator;
            else if (Flags.IsSet(TermFlags.IsDelimiter | TermFlags.IsPunctuation))
                tknType |= TokenType.Delimiter;
            var triggers = TokenTriggers.None;
            if (Flags.IsSet(TermFlags.IsBrace))
                triggers |= TokenTriggers.MatchBraces;
            if (Flags.IsSet(TermFlags.IsMemberSelect))
                triggers |= TokenTriggers.MemberSelect;
            var color = TokenColor.Text;
            if (Flags.IsSet(TermFlags.IsKeyword))
                color = TokenColor.Keyword;
            EditorInfo = new TokenEditorInfo(tknType, color, triggers);
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source)
        {
            if (!source.MatchSymbol(Text))
                return null;
            source.PreviewPosition += Text.Length;
            //In case of keywords, check that it is not followed by letter or digit
            if (Flags.IsSet(TermFlags.IsKeyword) && !AllowAlphaAfterKeyword)
            {
                var previewChar = source.PreviewChar;
                if (char.IsLetterOrDigit(previewChar) || previewChar == '_') return null; //reject
            }
            var token = source.CreateToken(OutputTerminal, Text);
            return token;
        }

        public override IList<string> GetFirsts()
        {
            return new[] {Text};
        }

        public override string ToString()
        {
            if (Name != Text) return Name;
            return Text;
        }

        public override string TokenToString(Token token)
        {
            var keyw = Flags.IsSet(TermFlags.IsKeyword) ? Resources.LabelKeyword : Resources.LabelKeySymbol;
                //"(Keyword)" : "(Key symbol)"
            var result = (token.ValueString ?? token.Text) + " " + keyw;
            return result;
        }

        #endregion
    } //class
}
using System.Collections.Generic;
using System.Diagnostics;

namespace Irony.Parsing
{
    public enum TokenFlags
    {
        IsIncomplete = 0x01
    }

    public enum TokenCategory
    {
        Content,
        Outline, //newLine, indent, dedent
        Comment,
        Directive,
        Error
    }

    public class TokenList : List<Token>
    {
    }

    public class TokenStack : Stack<Token>
    {
    }

    //Tokens are produced by scanner and fed to parser, optionally passing through Token filters in between. 
    public class Token
    {
        public readonly SourceLocation Location;
        public readonly string Text;
        public object Details;
        public TokenEditorInfo EditorInfo;
        public TokenFlags Flags;
        public KeyTerm KeyTerm;
        //matching opening/closing brace
        public Token OtherBrace;
        public short ScannerState; //Scanner state after producing token 
        public object Value;

        public Token(Terminal term, SourceLocation location, string text, object value)
        {
            SetTerminal(term);
            KeyTerm = term as KeyTerm;
            Location = location;
            Text = text;
            Value = value;
        }

        public Terminal Terminal { get; private set; }

        public string ValueString
        {
            get { return (Value == null ? string.Empty : Value.ToString()); }
        }

        public TokenCategory Category
        {
            get { return Terminal.Category; }
        }

        public int Length
        {
            get { return Text == null ? 0 : Text.Length; }
        }

        public void SetTerminal(Terminal terminal)
        {
            Terminal = terminal;
            EditorInfo = Terminal.EditorInfo; //set to term's EditorInfo by default
        }

        public bool IsSet(TokenFlags flag)
        {
            return (Flags & flag) != 0;
        }

        public bool IsError()
        {
            return Category == TokenCategory.Error;
        }

        [DebuggerStepThrough]
        public override string ToString()
        {
            return Terminal.TokenToString(this);
        } //method
    } //class

    //Some terminals may need to return a bunch of tokens in one call to TryMatch; MultiToken is a container for these tokens
    public class MultiToken : Token
    {
        public TokenList ChildTokens;

        public MultiToken(params Token[] tokens) : this(tokens[0].Terminal, tokens[0].Location, new TokenList())
        {
            ChildTokens.AddRange(tokens);
        }

        public MultiToken(Terminal term, SourceLocation location, TokenList childTokens)
            : base(term, location, string.Empty, null)
        {
            ChildTokens = childTokens;
        }
    } //class
} //namespace
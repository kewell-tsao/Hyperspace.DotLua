using System;
using System.Diagnostics;
#if DNXCORE50
using System.Globalization;
#endif

namespace Irony.Parsing
{
    public class SourceStream : ISourceStream
    {
        private readonly char[] _chars;
        private readonly StringComparison _stringComparison;
        private readonly int _tabWidth;
        private readonly int _textLength;

        public SourceStream(string text, bool caseSensitive, int tabWidth)
            : this(text, caseSensitive, tabWidth, new SourceLocation())
        {
        }

        public SourceStream(string text, bool caseSensitive, int tabWidth, SourceLocation initialLocation)
        {
            Text = text;
            _textLength = Text.Length;
            _chars = Text.ToCharArray();
#if DNXCORE50
            _stringComparison = caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
#else
            _stringComparison = caseSensitive
                ? StringComparison.InvariantCulture
                : StringComparison.InvariantCultureIgnoreCase;
#endif
            _tabWidth = tabWidth;
            Location = initialLocation;
            PreviewPosition = Location.Position;
            if (_tabWidth <= 1)
                _tabWidth = 8;
        }

        //returns substring from Location.Position till (PreviewPosition - 1)
        private string GetPreviewText()
        {
            var until = PreviewPosition;
            if (until > _textLength) until = _textLength;
            var p = Location.Position;
            var text = Text.Substring(p, until - p);
            return text;
        }

        // To make debugging easier: show 20 chars from current position
        public override string ToString()
        {
            string result;
            try
            {
                var p = Location.Position;
                if (p + 20 < _textLength)
                    result = Text.Substring(p, 20) + Resources.LabelSrcHaveMore; // " ..."
                else
                    result = Text.Substring(p) + Resources.LabelEofMark; //"(EOF)"
            }
            catch (Exception)
            {
                result = PreviewChar + Resources.LabelSrcHaveMore;
            }
            return string.Format(Resources.MsgSrcPosToString, result, Location); //"[{0}], at {1}"
        }

        //Computes the Location info (line, col) for a new source position.
        private void SetNewPosition(int newPosition)
        {
            if (newPosition < Position)
                throw new Exception(Resources.ErrCannotMoveBackInSource);
            var p = Position;
            var col = Location.Column;
            var line = Location.Line;
            while (p < newPosition)
            {
                if (p >= _textLength)
                    break;
                var curr = _chars[p];
                switch (curr)
                {
                    case '\n':
                        line++;
                        col = 0;
                        break;
                    case '\r':
                        break;
                    case '\t':
                        col = (col/_tabWidth + 1)*_tabWidth;
                        break;
                    default:
                        col++;
                        break;
                } //switch
                p++;
            }
            Location = new SourceLocation(p, line, col);
        }

        #region ISourceStream Members

        public string Text { get; }

        public int Position
        {
            get { return Location.Position; }
            set
            {
                if (Location.Position != value)
                    SetNewPosition(value);
            }
        }

        public SourceLocation Location { [DebuggerStepThrough] get; set; }

        public int PreviewPosition { get; set; }

        public char PreviewChar
        {
            [DebuggerStepThrough]
            get
            {
                if (PreviewPosition >= _textLength)
                    return '\0';
                return _chars[PreviewPosition];
            }
        }

        public char NextPreviewChar
        {
            [DebuggerStepThrough]
            get
            {
                if (PreviewPosition + 1 >= _textLength) return '\0';
                return _chars[PreviewPosition + 1];
            }
        }

        public bool MatchSymbol(string symbol)
        {
            try
            {
#if DNXCORE50
                var stringComparer = CultureInfo.InvariantCulture.CompareInfo.GetStringComparer(_stringComparison == StringComparison.CurrentCultureIgnoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
                int cmp = stringComparer.Compare(Text.Substring(PreviewPosition, symbol.Length), symbol);
#else
                var cmp = string.Compare(Text, PreviewPosition, symbol, 0, symbol.Length, _stringComparison);
#endif
                return cmp == 0;
            }
            catch
            {
                //exception may be thrown if Position + symbol.length > text.Length; 
                // this happens not often, only at the very end of the file, so we don't check this explicitly
                //but simply catch the exception and return false. Again, try/catch block has no overhead
                // if exception is not thrown. 
                return false;
            }
        }

        public Token CreateToken(Terminal terminal)
        {
            var tokenText = GetPreviewText();
            return new Token(terminal, Location, tokenText, tokenText);
        }

        public Token CreateToken(Terminal terminal, object value)
        {
            var tokenText = GetPreviewText();
            return new Token(terminal, Location, tokenText, value);
        }

        [DebuggerStepThrough]
        public bool EOF()
        {
            return PreviewPosition >= _textLength;
        }

        #endregion
    } //class
} //namespace
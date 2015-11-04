namespace Irony.Parsing
{
    public struct SourceLocation
    {
        /// <summary>Source column number, 0-based.</summary>
        public int Column;

        /// <summary>Source line number, 0-based.</summary>
        public int Line;

        public int Position;

        public SourceLocation(int position, int line, int column)
        {
            Position = position;
            Line = line;
            Column = column;
        }

        public static SourceLocation Empty { get; } = new SourceLocation();
        //Line/col are zero-based internally
        public override string ToString()
        {
            return string.Format(Resources.FmtRowCol, Line + 1, Column + 1);
        }

        //Line and Column displayed to user should be 1-based
        public string ToUiString()
        {
            return string.Format(Resources.FmtRowCol, Line + 1, Column + 1);
        }

        public static int Compare(SourceLocation x, SourceLocation y)
        {
            if (x.Position < y.Position) return -1;
            if (x.Position == y.Position) return 0;
            return 1;
        }

        public static SourceLocation operator +(SourceLocation x, SourceLocation y)
        {
            return new SourceLocation(x.Position + y.Position, x.Line + y.Line, x.Column + y.Column);
        }

        public static SourceLocation operator +(SourceLocation x, int offset)
        {
            return new SourceLocation(x.Position + offset, x.Line, x.Column + offset);
        }
    } //SourceLocation

    public struct SourceSpan
    {
        public readonly int Length;
        public readonly SourceLocation Location;

        public SourceSpan(SourceLocation location, int length)
        {
            Location = location;
            Length = length;
        }

        public int EndPosition
        {
            get { return Location.Position + Length; }
        }

        public bool InRange(int position)
        {
            return (position >= Location.Position && position <= EndPosition);
        }
    }
} //namespace
using System;

namespace Irony.Parsing
{
    //A terminal for representing fixed-length lexemes coming up sometimes in programming language
    // (in Fortran for ex, every line starts with 5-char label, followed by a single continuation char)
    // It may be also used to create grammar/parser for reading data files with fixed length fields
    public class FixedLengthLiteral : DataLiteralBase
    {
        public int Length;

        public FixedLengthLiteral(string name, int length, TypeCode dataType) : base(name, dataType)
        {
            Length = length;
        }

        protected override string ReadBody(ParsingContext context, ISourceStream source)
        {
            source.PreviewPosition = source.Location.Position + Length;
            var body = source.Text.Substring(source.Location.Position, Length);
            return body;
        }
    } //class
} //namespace
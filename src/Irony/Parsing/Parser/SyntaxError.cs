using System.Collections.Generic;

namespace Irony.Parsing
{
    //Container for syntax error
    public class SyntaxError
    {
        public readonly SourceLocation Location;
        public readonly string Message;
        public ParserState ParserState;

        public SyntaxError(SourceLocation location, string message, ParserState parserState)
        {
            Location = location;
            Message = message;
            ParserState = parserState;
        }

        public override string ToString()
        {
            return Message;
        }
    } //class

    public class SyntaxErrorList : List<SyntaxError>
    {
        public static int ByLocation(SyntaxError x, SyntaxError y)
        {
            return SourceLocation.Compare(x.Location, y.Location);
        }
    }
} //namespace
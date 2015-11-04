using System.Collections.Generic;
using Irony.Parsing;

namespace Irony
{
    public enum ErrorLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    //Container for syntax errors and warnings
    public class LogMessage
    {
        public readonly ErrorLevel Level;
        public readonly SourceLocation Location;
        public readonly string Message;
        public readonly ParserState ParserState;

        public LogMessage(ErrorLevel level, SourceLocation location, string message, ParserState parserState)
        {
            Level = level;
            Location = location;
            Message = message;
            ParserState = parserState;
        }

        public override string ToString()
        {
            return Message;
        }
    } //class

    public class LogMessageList : List<LogMessage>
    {
        public static int ByLocation(LogMessage x, LogMessage y)
        {
            return SourceLocation.Compare(x.Location, y.Location);
        }
    }
} //namespace
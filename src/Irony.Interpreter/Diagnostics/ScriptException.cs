using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;

namespace Irony.Interpreter
{
    public class ScriptException : Exception
    {
        public SourceLocation Location;
        public ScriptStackTrace ScriptStackTrace;
        public ScriptException(string message) : base(message) { }
        public ScriptException(string message, Exception inner) : base(message, inner) { }
        public ScriptException(string message, Exception inner, SourceLocation location, ScriptStackTrace stack)
               : base(message, inner)
        {
            Location = location;
            ScriptStackTrace = stack;
        }

        public override string ToString()
        {
            return Message + Environment.NewLine + ScriptStackTrace.ToString();
        }
    }//class

}

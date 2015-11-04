using System;
using System.Collections.Generic;

namespace Irony.Parsing
{
    public class ParserTraceEntry
    {
        public ParseTreeNode Input;
        public bool IsError;
        public string Message;
        public ParseTreeNode StackTop;
        public ParserState State;

        public ParserTraceEntry(ParserState state, ParseTreeNode stackTop, ParseTreeNode input, string message,
            bool isError)
        {
            State = state;
            StackTop = stackTop;
            Input = input;
            Message = message;
            IsError = isError;
        }
    } //class

    public class ParserTrace : List<ParserTraceEntry>
    {
    }

    public class ParserTraceEventArgs : EventArgs
    {
        public readonly ParserTraceEntry Entry;

        public ParserTraceEventArgs(ParserTraceEntry entry)
        {
            Entry = entry;
        }

        public override string ToString()
        {
            return Entry.ToString();
        }
    } //class
} //namespace
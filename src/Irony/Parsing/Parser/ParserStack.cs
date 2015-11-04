using System.Collections.Generic;

namespace Irony.Parsing
{
    public class ParserStack : List<ParseTreeNode>
    {
        public ParserStack() : base(200)
        {
        }

        public ParseTreeNode Top
        {
            get
            {
                if (Count == 0) return null;
                return base[Count - 1];
            }
        }

        public void Push(ParseTreeNode nodeInfo)
        {
            Add(nodeInfo);
        }

        public void Push(ParseTreeNode nodeInfo, ParserState state)
        {
            nodeInfo.State = state;
            Add(nodeInfo);
        }

        public ParseTreeNode Pop()
        {
            var top = Top;
            RemoveAt(Count - 1);
            return top;
        }

        public void Pop(int count)
        {
            RemoveRange(Count - count, count);
        }

        public void PopUntil(int finalCount)
        {
            if (finalCount < Count)
                Pop(Count - finalCount);
        }
    }
}
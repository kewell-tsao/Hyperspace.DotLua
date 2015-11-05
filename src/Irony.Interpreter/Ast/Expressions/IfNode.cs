using System;
using System.Collections.Generic;
using System.Text;

using Irony.Ast;
using Irony.Parsing;

namespace Irony.Interpreter.Ast
{
    public class IfNode : AstNode
    {
        public AstNode Test;
        public AstNode IfTrue;
        public AstNode IfFalse;

        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            Test = AddChild("Test", nodes[0]);
            IfTrue = AddChild("IfTrue", nodes[1]);
            if (nodes.Count > 2)
                IfFalse = AddChild("IfFalse", nodes[2]);
        }

        protected override object DoEvaluate(ScriptThread thread)
        {
            thread.CurrentNode = this;  //standard prolog
            object result = null;
            var test = Test.Evaluate(thread);
            var isTrue = thread.Runtime.IsTrue(test);
            if (isTrue)
            {
                if (IfTrue != null)
                    result = IfTrue.Evaluate(thread);
            }
            else
            {
                if (IfFalse != null)
                    result = IfFalse.Evaluate(thread);
            }
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        public override void SetIsTail()
        {
            base.SetIsTail();
            if (IfTrue != null)
                IfTrue.SetIsTail();
            if (IfFalse != null)
                IfFalse.SetIsTail();
        }

    }//class

}//namespace

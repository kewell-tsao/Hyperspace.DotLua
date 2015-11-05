using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Irony.Ast;
using Irony.Parsing;

namespace Irony.Interpreter.Ast
{

    //A node representing expression list - for example, list of argument expressions in function call
    public class ExpressionListNode : AstNode
    {

        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            foreach (var child in treeNode.ChildNodes)
            {
                AddChild(NodeUseType.Parameter, "expr", child);
            }
            AsString = "Expression list";
        }

        protected override object DoEvaluate(ScriptThread thread)
        {
            thread.CurrentNode = this;  //standard prolog
            var values = new object[ChildNodes.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ChildNodes[i].Evaluate(thread);
            }
            thread.CurrentNode = Parent; //standard epilog
            return values;
        }

    }//class

}//namespace

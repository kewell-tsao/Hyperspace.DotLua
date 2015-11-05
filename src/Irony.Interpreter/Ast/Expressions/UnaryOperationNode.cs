using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

using Irony.Ast;
using Irony.Parsing;

namespace Irony.Interpreter.Ast
{

    public class UnaryOperationNode : AstNode
    {
        public string OpSymbol;
        public AstNode Argument;
        private OperatorImplementation _lastUsed;

        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            OpSymbol = nodes[0].FindTokenAndGetText();
            Argument = AddChild("Arg", nodes[1]);
            base.AsString = OpSymbol + "(unary op)";
            var interpContext = (InterpreterAstContext)context;
            base.ExpressionType = interpContext.OperatorHandler.GetUnaryOperatorExpressionType(OpSymbol);
        }

        protected override object DoEvaluate(ScriptThread thread)
        {
            thread.CurrentNode = this;  //standard prolog
            var arg = Argument.Evaluate(thread);
            var result = thread.Runtime.ExecuteUnaryOperator(base.ExpressionType, arg, ref _lastUsed);
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        public override void SetIsTail()
        {
            base.SetIsTail();
            Argument.SetIsTail();
        }

    }//class
}//namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using Irony.Parsing;
using Irony.Ast;

namespace Irony.Interpreter.Ast
{

    public class IdentifierNode : AstNode
    {
        public string Symbol;
        private Binding _accessor;

        public IdentifierNode() { }

        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            Symbol = treeNode.Token.ValueString;
            AsString = Symbol;
        }

        //Executed only once, on the first call
        protected override object DoEvaluate(ScriptThread thread)
        {
            thread.CurrentNode = this;  //standard prolog
            _accessor = thread.Bind(Symbol, BindingRequestFlags.Read);
            this.Evaluate = _accessor.GetValueRef; // Optimization - directly set method ref to accessor's method. EvaluateReader;
            var result = this.Evaluate(thread);
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        public override void DoSetValue(ScriptThread thread, object value)
        {
            thread.CurrentNode = this;  //standard prolog
            if (_accessor == null)
            {
                _accessor = thread.Bind(Symbol, BindingRequestFlags.Write | BindingRequestFlags.ExistingOrNew);
            }
            _accessor.SetValueRef(thread, value);
            thread.CurrentNode = Parent;  //standard epilog
        }

    }//class
}//namespace

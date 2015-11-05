using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Irony.Parsing;
using Irony.Interpreter.Ast;

namespace Irony.Interpreter
{

    public class Scope : ScopeBase
    {
        public object[] Parameters;
        public Scope Caller;
        public Scope Creator; //either caller or closure parent
        private Scope _parent; //computed on demand

        public Scope(ScopeInfo scopeInfo, Scope caller, Scope creator, object[] parameters) : base(scopeInfo)
        {
            Caller = caller;
            Creator = creator;
            Parameters = parameters;
        }

        public object[] GetParameters()
        {
            return Parameters;
        }

        public object GetParameter(int index)
        {
            return Parameters[index];
        }
        public void SetParameter(int index, object value)
        {
            Parameters[index] = value;
        }

        // Lexical parent, computed on demand
        public Scope Parent
        {
            get
            {
                if (_parent == null)
                    _parent = GetParent();
                return _parent;
            }
            set { _parent = value; }
        }

        protected Scope GetParent()
        {
            // Walk along creators chain and find a scope with ScopeInfo matching this.ScopeInfo.Parent
            var parentScopeInfo = Info.Parent;
            if (parentScopeInfo == null)
                return null;
            var current = Creator;
            while (current != null)
            {
                if (current.Info == parentScopeInfo)
                    return current;
                current = current.Creator;
            }
            return null;
        }// method

    }//class

}//namespace

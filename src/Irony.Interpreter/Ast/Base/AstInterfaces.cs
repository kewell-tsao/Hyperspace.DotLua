using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Irony.Interpreter.Ast
{

    //This interface is expected by Irony's Gramamr Explorer. 
    public interface ICallTarget
    {
        object Call(ScriptThread thread, object[] parameters);
    }

    //Simple visitor interface
    public interface IAstVisitor
    {
        void BeginVisit(IVisitableNode node);
        void EndVisit(IVisitableNode node);
    }

    public interface IVisitableNode
    {
        void AcceptVisitor(IAstVisitor visitor);
    }

    public interface IOperatorHelper
    {
        ExpressionType GetOperatorExpressionType(string symbol);
        ExpressionType GetUnaryOperatorExpressionType(string symbol);

    }
}

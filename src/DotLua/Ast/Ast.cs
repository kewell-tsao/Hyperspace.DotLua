using System.Collections.Generic;

namespace DotLua.Ast
{
    public enum BinaryOp
    {
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Power,
        Modulo,
        Concat,
        GreaterThan,
        GreaterOrEqual,
        LessThan,
        LessOrEqual,
        Equal,
        Different,
        And,
        Or
    }

    public enum UnaryOp
    {
        Negate,
        Invert,
        Length
    }

    public abstract class AstElement
    {
        public int lineNumber, columnNumber;
        public string filename;
    }

    public interface IStatement
    {
    }

    public interface IExpression
    {
    }

    public interface ILiteral
    {
        LuaObject GetValue();
    }

    public interface IAssignable : IExpression
    {
    }

    public class Variable : AstElement, IExpression, IAssignable
    {
        // Prefix.Name
        //public IExpression Prefix;
        public string Name;
    }

    public class Argument : AstElement
    {
        public string Name;
    }

    public class StringLiteral : AstElement, IExpression
    {
        public string Value;

        public LuaObject GetValue()
        {
            return LuaObject.FromString(Value);
        }
    }

    public class NumberLiteral : AstElement, IExpression
    {
        public double Value;

        public LuaObject GetValue()
        {
            return LuaObject.FromNumber(Value);
        }
    }

    public class NilLiteral : AstElement, IExpression
    {
        public LuaObject GetValue()
        {
            return LuaObject.Nil;
        }
    }

    public class BoolLiteral : AstElement, IExpression
    {
        public bool Value;

        public LuaObject GetValue()
        {
            return LuaObject.FromBool(Value);
        }
    }

    public class VarargsLiteral : AstElement, IExpression
    {
    }

    public class FunctionCall : AstElement, IStatement, IExpression
    {
        public IExpression Function;
        public List<IExpression> Arguments = new List<IExpression>();
    }

    public class TableAccess : AstElement, IExpression, IAssignable
    {
        // Expression[Index]
        public IExpression Expression;
        public IExpression Index;
    }

    public class FunctionDefinition : AstElement, IExpression
    {
        // function(Arguments) Body end
        public List<Argument> Arguments = new List<Argument>();
        public bool isVarargs;
        public Block Body;
    }

    public class BinaryExpression : AstElement, IExpression
    {
        public IExpression Left, Right;
        public BinaryOp Operation;
    }

    public class UnaryExpression : AstElement, IExpression
    {
        public IExpression Expression;
        public UnaryOp Operation;
    }

    public class TableConstructor : AstElement, IExpression
    {
        public Dictionary<IExpression, IExpression> Values = new Dictionary<IExpression, IExpression>();
    }

    public class Assignment : AstElement, IStatement
    {
        // Var1, Var2, Var3 = Exp1, Exp2, Exp3
        //public Variable[] Variables;
        //public IExpression[] Expressions;

        public List<IAssignable> Variables = new List<IAssignable>();
        public List<IExpression> Expressions = new List<IExpression>();
    }

    public class ReturnStat : AstElement, IStatement
    {
        public List<IExpression> Expressions = new List<IExpression>();
    }

    public class BreakStat : AstElement, IStatement
    {
    }

    public class LocalAssignment : AstElement, IStatement
    {
        public List<string> Names = new List<string>();
        public List<IExpression> Values = new List<IExpression>();
    }

    public class Block : AstElement, IStatement
    {
        public List<IStatement> Statements = new List<IStatement>();
    }

    public class WhileStat : AstElement, IStatement
    {
        public Block Block;
        public IExpression Condition;
    }

    public class RepeatStat : AstElement, IStatement
    {
        public Block Block;
        public IExpression Condition;
    }

    public class NumericFor : AstElement, IStatement
    {
        public Block Block;
        public IExpression Var, Limit, Step;
        public string Variable;
    }

    public class GenericFor : AstElement, IStatement
    {
        public Block Block;
        public List<IExpression> Expressions = new List<IExpression>();
        public List<string> Variables = new List<string>();
    }

    public class IfStat : AstElement, IStatement
    {
        public Block Block;
        public IExpression Condition;
        public Block ElseBlock;
        public List<IfStat> ElseIfs = new List<IfStat>();
    }
}
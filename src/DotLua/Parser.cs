using System;
using System.Collections.Generic;
using System.IO;
using DotLua.Ast;
using Irony.Parsing;
using NumberLiteral = DotLua.Ast.NumberLiteral;
using StringLiteral = DotLua.Ast.StringLiteral;

namespace DotLua
{
    public class Parser
    {
        private readonly LuaGrammar grammar = new LuaGrammar();
        private readonly LanguageData language;
        private readonly Irony.Parsing.Parser parser;
        public Block block;

        public Parser()
        {
            language = new LanguageData(grammar);
            parser = new Irony.Parsing.Parser(language);
        }

        public Block ParseString(string Chunk)
        {
            var parseTree = parser.Parse(Chunk);
            var root = parseTree.Root;
            if (root == null)
            {
                var msg = parseTree.ParserMessages[0];
                throw new LuaException("", msg.Location.Line, msg.Location.Column, msg.Message);
            }
            return (ParseBlock(root));
        }

        public Block ParseFile(string Filename)
        {
            var source = File.ReadAllText(Filename);
            var parseTree = parser.Parse(source, Filename);
            var root = parseTree.Root;
            if (root == null)
            {
                var msg = parseTree.ParserMessages[0];
                throw new LuaException(Filename, msg.Location.Line, msg.Location.Column, msg.Message);
            }
            return (ParseBlock(root));
        }

        private Block ParseBlock(ParseTreeNode node)
        {
            var block = new Block();
            block.Statements = new List<IStatement>();
            foreach (var child in node.ChildNodes)
            {
                switch (child.Term.Name)
                {
                    case "Assignment":
                        block.Statements.Add(ParseAssign(child));
                        break;
                    case "LocalAssignment":
                        block.Statements.Add(ParseLocalAssign(child));
                        break;
                    case "FunctionCall":
                        block.Statements.Add(ParseFunctionCall(child));
                        break;
                    case "ReturnStat":
                        block.Statements.Add(ParseReturnStat(child));
                        break;
                    case "BreakStat":
                        block.Statements.Add(new BreakStat());
                        break;
                    case "DoBlock":
                        block.Statements.Add(ParseDoBlock(child));
                        break;
                    case "If":
                        block.Statements.Add(ParseIf(child));
                        break;
                    case "While":
                        block.Statements.Add(ParseWhile(child));
                        break;
                    case "Repeat":
                        block.Statements.Add(ParseRepeat(child));
                        break;
                    case "FunctionDecl":
                        block.Statements.Add(ParseFunctionDecl(child));
                        break;
                    case "For":
                        block.Statements.Add(ParseFor(child));
                        break;
                    case "OopCall":
                        block.Statements.Add(ParseOopCall(child));
                        break;
                    case ";":
                        break;
                    default:
                        throw new NotImplementedException("Node not yet implemented");
                }
            }
            return block;
        }

        private IStatement ParseFor(ParseTreeNode node)
        {
            if (node.Term.Name == "For")
            {
                var block = ParseDoBlock(node.ChildNodes[2]);
                var type = node.ChildNodes[1].ChildNodes[0];
                if (type.Term.Name == "NumericFor")
                {
                    var cycle = new NumericFor();
                    cycle.Block = block;
                    cycle.Variable = type.ChildNodes[0].Token.ValueString;
                    cycle.Var = ParseExpression(type.ChildNodes[1]);
                    cycle.Limit = ParseExpression(type.ChildNodes[2]);
                    cycle.Step = new NumberLiteral { Value = 1 };
                    if (type.ChildNodes[3].ChildNodes.Count > 0)
                    {
                        var child = type.ChildNodes[3].ChildNodes[0];
                        cycle.Step = ParseExpression(child);
                    }

                    return cycle;
                }
                else
                {
                    var cycle = new GenericFor();
                    cycle.Block = block;

                    var nameList = type.ChildNodes[0];
                    var exprList = type.ChildNodes[2];

                    while (true)
                    {
                        var name = nameList.ChildNodes[0].Token.ValueString;
                        cycle.Variables.Add(name);
                        var child = nameList.ChildNodes[1];
                        if (child.ChildNodes.Count > 0)
                            nameList = child.ChildNodes[0];
                        else
                            break;
                    }

                    while (true)
                    {
                        var expr = ParseExpression(exprList.ChildNodes[0]);
                        cycle.Expressions.Add(expr);
                        var child = exprList.ChildNodes[1];
                        if (child.ChildNodes.Count > 0)
                            exprList = child.ChildNodes[0];
                        else
                            break;
                    }

                    return cycle;
                }
            }
            throw new Exception("Invalid For node");
        }

        #region Binary expression tree

        private IExpression ParseOrOp(ParseTreeNode node)
        {
            if (node.Term.Name == "OrOp")
            {
                var left = node.ChildNodes[0];
                var lexpr = ParseAndOp(left);
                if (node.ChildNodes[1].ChildNodes.Count == 0)
                    return lexpr;
                var right = node.ChildNodes[1].ChildNodes[1];
                var rexpr = ParseAndOp(right);

                return new BinaryExpression { Left = lexpr, Right = rexpr, Operation = BinaryOp.Or };
            }
            throw new Exception("Invalid OrOp node");
        }

        private IExpression ParseAndOp(ParseTreeNode node)
        {
            if (node.Term.Name == "AndOp")
            {
                var left = node.ChildNodes[0];
                var lexpr = ParseRelOp(left);
                if (node.ChildNodes[1].ChildNodes.Count == 0)
                    return lexpr;
                var right = node.ChildNodes[1].ChildNodes[1];
                var rexpr = ParseRelOp(right);

                return new BinaryExpression { Left = lexpr, Right = rexpr, Operation = BinaryOp.And };
            }
            throw new Exception("Invalid AndOp node");
        }

        private IExpression ParseRelOp(ParseTreeNode node)
        {
            if (node.Term.Name == "RelOp")
            {
                var left = node.ChildNodes[0];
                var lexpr = ParseConcatOp(left);
                if (node.ChildNodes[1].ChildNodes.Count == 0)
                    return lexpr;
                var opstring = node.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.ValueString;
                BinaryOp op;
                switch (opstring)
                {
                    case ">":
                        op = BinaryOp.GreaterThan;
                        break;
                    case ">=":
                        op = BinaryOp.GreaterOrEqual;
                        break;
                    case "<":
                        op = BinaryOp.LessThan;
                        break;
                    case "<=":
                        op = BinaryOp.LessOrEqual;
                        break;
                    case "==":
                        op = BinaryOp.Equal;
                        break;
                    case "~=":
                        op = BinaryOp.Different;
                        break;
                    default:
                        throw new Exception("Invalid operator");
                }

                var right = node.ChildNodes[1].ChildNodes[1];
                var rexpr = ParseConcatOp(right);

                return new BinaryExpression { Left = lexpr, Right = rexpr, Operation = op };
            }
            throw new Exception("Invalid RelOp node");
        }

        private IExpression ParseConcatOp(ParseTreeNode node)
        {
            if (node.Term.Name == "ConcatOp")
            {
                var left = node.ChildNodes[0];
                var lexpr = ParseAddOp(left);
                if (node.ChildNodes[1].ChildNodes.Count == 0)
                    return lexpr;
                var opstring = node.ChildNodes[1].ChildNodes[0].Token.ValueString;
                var op = BinaryOp.Concat;

                var right = node.ChildNodes[1].ChildNodes[1];
                var rexpr = ParseAddOp(right);

                return new BinaryExpression { Left = lexpr, Right = rexpr, Operation = op };
            }
            throw new Exception("Invalid ConcatOp node");
        }

        private IExpression ParseAddOp(ParseTreeNode node)
        {
            if (node.Term.Name == "AddOp")
            {
                var left = node.ChildNodes[0];
                var lexpr = ParseMulOp(left);
                if (node.ChildNodes[1].ChildNodes.Count == 0)
                    return lexpr;
                var opstring = node.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.ValueString;
                BinaryOp op;
                switch (opstring)
                {
                    case "+":
                        op = BinaryOp.Addition;
                        break;
                    case "-":
                        op = BinaryOp.Subtraction;
                        break;
                    default:
                        throw new Exception("Invalid operator");
                }

                var right = node.ChildNodes[1].ChildNodes[1];
                var rexpr = ParseMulOp(right);

                return new BinaryExpression { Left = lexpr, Right = rexpr, Operation = op };
            }
            throw new Exception("Invalid AddOp node");
        }

        private IExpression ParseMulOp(ParseTreeNode node)
        {
            if (node.Term.Name == "MulOp")
            {
                var left = node.ChildNodes[0];
                var lexpr = ParsePowerOp(left);
                if (node.ChildNodes[1].ChildNodes.Count == 0)
                    return lexpr;
                var opstring = node.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.ValueString;
                BinaryOp op;
                switch (opstring)
                {
                    case "*":
                        op = BinaryOp.Multiplication;
                        break;
                    case "/":
                        op = BinaryOp.Division;
                        break;
                    case "%":
                        op = BinaryOp.Modulo;
                        break;
                    default:
                        throw new Exception("Invalid operator");
                }

                var right = node.ChildNodes[1].ChildNodes[1];
                var rexpr = ParsePowerOp(right);

                return new BinaryExpression { Left = lexpr, Right = rexpr, Operation = op };
            }
            throw new Exception("Invalid MulOp node");
        }

        private IExpression ParsePowerOp(ParseTreeNode node)
        {
            if (node.Term.Name == "PowerOp")
            {
                var left = node.ChildNodes[0];
                var lexpr = ParseExpression(left);
                if (node.ChildNodes[1].ChildNodes.Count == 0)
                    return lexpr;

                var right = node.ChildNodes[1].ChildNodes[1];
                var rexpr = ParseExpression(right);

                return new BinaryExpression { Left = lexpr, Right = rexpr, Operation = BinaryOp.Power };
            }
            throw new Exception("Invalid PowerOp node");
        }

        #endregion

        #region Statements

        private FunctionCall ParseFunctionCall(ParseTreeNode node)
        {
            if (node.Term.Name == "FunctionCall")
            {
                var expr = ParsePrefix(node.ChildNodes[0]);
                var call = new FunctionCall();
                call.Arguments = new List<IExpression>();
                call.Function = expr;

                var root = node.ChildNodes[1].ChildNodes[0];
                if (root.ChildNodes.Count != 0)
                {
                    root = root.ChildNodes[0];
                    while (true)
                    {
                        call.Arguments.Add(ParseExpression(root.ChildNodes[0]));
                        if (root.ChildNodes.Count == 1)
                            break;
                        root = root.ChildNodes[1];
                    }
                }
                return call;
            }
            throw new Exception("Invalid FunctionCall node");
        }

        private FunctionCall ParseOopCall(ParseTreeNode node)
        {
            if (node.Term.Name == "OopCall")
            {
                var expr = ParsePrefix(node.ChildNodes[0]);
                var name = node.ChildNodes[1].Token.ValueString;
                var call = new FunctionCall();
                call.Arguments.Add(expr);
                call.Function = new Variable { Name = name, Prefix = expr };

                var root = node.ChildNodes[2].ChildNodes[0];
                if (root.ChildNodes.Count != 0)
                {
                    root = root.ChildNodes[0];
                    while (true)
                    {
                        call.Arguments.Add(ParseExpression(root.ChildNodes[0]));
                        if (root.ChildNodes.Count == 1)
                            break;
                        root = root.ChildNodes[1];
                    }
                }
                return call;
            }
            throw new Exception("Invalid OopCall node");
        }

        private Assignment ParseAssign(ParseTreeNode node)
        {
            if (node.Term.Name == "Assignment")
            {
                var assign = new Assignment();

                var left = node.ChildNodes[0];
                var right = node.ChildNodes[1];

                assign.Variables.Add(ParseVariable(left.ChildNodes[0]));
                assign.Expressions.Add(ParseExpression(right.ChildNodes[0]));

                left = left.ChildNodes[1];
                right = right.ChildNodes[1];

                while (left.ChildNodes.Count > 0)
                {
                    left = left.ChildNodes[0];
                    assign.Variables.Add(ParseVariable(left.ChildNodes[0]));
                    left = left.ChildNodes[1];
                }

                while (right.ChildNodes.Count > 0)
                {
                    right = right.ChildNodes[0];
                    assign.Expressions.Add(ParseExpression(right.ChildNodes[0]));
                    right = right.ChildNodes[1];
                }

                return assign;
            }
            throw new Exception("Invalid Assignment node");
        }

        private LocalAssignment ParseLocalAssign(ParseTreeNode node)
        {
            if (node.Term.Name == "LocalAssignment")
            {
                var assign = new LocalAssignment();


                var child = node.ChildNodes[1];

                if (child.ChildNodes[0].Term.Name == "LocalFunction")
                {
                    child = child.ChildNodes[0];

                    var argsNode = child.ChildNodes[2];
                    var blockNode = child.ChildNodes[3];

                    assign.Names.Add(child.ChildNodes[1].Token.ValueString);
                    var func = new FunctionDefinition();

                    if (argsNode.ChildNodes.Count > 0)
                    {
                        argsNode = argsNode.ChildNodes[0];
                        while (argsNode.ChildNodes.Count > 0)
                        {
                            var ident = argsNode.ChildNodes[0].Token.ValueString;
                            func.Arguments.Add(new Argument { Name = ident });
                            if (argsNode.ChildNodes.Count == 1)
                                break;
                            argsNode = argsNode.ChildNodes[1];
                        }
                    }

                    func.Body = ParseBlock(blockNode);

                    assign.Values.Add(func);
                    return assign;
                }

                var left = child.ChildNodes[0];
                var right = child.ChildNodes[1];

                assign.Names.Add(left.ChildNodes[0].Token.ValueString);

                left = left.ChildNodes[1];

                while (left.ChildNodes.Count > 0)
                {
                    left = left.ChildNodes[0];
                    assign.Names.Add(left.ChildNodes[0].Token.ValueString);
                    left = left.ChildNodes[1];
                }

                while (right.ChildNodes.Count > 0)
                {
                    right = right.ChildNodes[0];
                    assign.Values.Add(ParseExpression(right.ChildNodes[0]));
                    right = right.ChildNodes[1];
                }

                return assign;
            }
            throw new Exception("Invalid LocalAssignment node");
        }

        private ReturnStat ParseReturnStat(ParseTreeNode node)
        {
            if (node.Term.Name == "ReturnStat")
            {
                var ret = new ReturnStat();
                var child = node.ChildNodes[1];
                while (child.ChildNodes.Count > 0)
                {
                    child = child.ChildNodes[0];
                    ret.Expressions.Add(ParseExpression(child.ChildNodes[0]));
                    child = child.ChildNodes[1];
                }
                return ret;
            }
            throw new Exception("Invalid ReturnStat node");
        }

        private Block ParseDoBlock(ParseTreeNode node)
        {
            if (node.Term.Name == "DoBlock")
                return ParseBlock(node.ChildNodes[1]);
            throw new Exception("Invalid DoBlock node");
        }

        private RepeatStat ParseRepeat(ParseTreeNode node)
        {
            if (node.Term.Name == "Repeat")
            {
                var block = ParseBlock(node.ChildNodes[1]);
                var condition = ParseExpression(node.ChildNodes[3]);

                return new RepeatStat
                {
                    Block = block,
                    Condition = condition
                };
            }
            throw new Exception("Invalid Repeat node");
        }

        private WhileStat ParseWhile(ParseTreeNode node)
        {
            if (node.Term.Name == "While")
            {
                return new WhileStat
                {
                    Condition = ParseExpression(node.ChildNodes[1]),
                    Block = ParseDoBlock(node.ChildNodes[2])
                };
            }
            throw new Exception("Invalid While node");
        }

        private IfStat ParseIf(ParseTreeNode node)
        {
            if (node.Term.Name == "If")
            {
                var condition = ParseExpression(node.ChildNodes[1]);
                var block = ParseBlock(node.ChildNodes[3]);

                var If = new IfStat();
                If.Block = block;
                If.Condition = condition;
                If.ElseIfs = new List<IfStat>();

                var ElseifNode = node.ChildNodes[4];
                var ElseNode = node.ChildNodes[5];

                while (ElseifNode.ChildNodes.Count != 0)
                {
                    var childnode = ElseifNode.ChildNodes[0];
                    var elseif = new IfStat();
                    elseif.Condition = ParseExpression(childnode.ChildNodes[1]);
                    elseif.Block = ParseBlock(childnode.ChildNodes[3]);

                    If.ElseIfs.Add(elseif);

                    ElseifNode = childnode.ChildNodes[4];
                }

                if (ElseNode.ChildNodes.Count != 0)
                    If.ElseBlock = ParseBlock(ElseNode.ChildNodes[0].ChildNodes[1]);

                return If;
            }
            throw new Exception("Invalid If node");
        }

        private Assignment ParseFunctionDecl(ParseTreeNode node)
        {
            if (node.Term.Name == "FunctionDecl")
            {
                var expr = ParseVariable(node.ChildNodes[1]);

                var argsNode = node.ChildNodes[3].ChildNodes[0];
                var chunkNode = node.ChildNodes[4];

                var block = ParseBlock(chunkNode);
                var def = new FunctionDefinition();
                def.Arguments = new List<Argument>();

                var nameNode = node.ChildNodes[2];
                if (nameNode.ChildNodes.Count > 0)
                {
                    def.Arguments.Add(new Argument { Name = "self" });
                    expr = new Variable
                    {
                        Name = nameNode.ChildNodes[0].Token.ValueString,
                        Prefix = expr
                    };
                }
                def.Body = block;
                if (argsNode.ChildNodes.Count > 0)
                {
                    argsNode = argsNode.ChildNodes[0];
                    while (argsNode.ChildNodes.Count > 0)
                    {
                        var ident = argsNode.ChildNodes[0].Token.ValueString;
                        def.Arguments.Add(new Argument { Name = ident });
                        if (argsNode.ChildNodes.Count == 1)
                            break;
                        argsNode = argsNode.ChildNodes[1];
                    }
                }
                var assign = new Assignment();
                assign.Variables.Add(expr);
                assign.Expressions.Add(def);
                return assign;
            }
            throw new Exception("Invalid FunctionDecl node");
        }

        #endregion

        #region Expressions

        private UnaryExpression ParseUnaryExpr(ParseTreeNode node)
        {
            if (node.Term.Name == "UnaryExpr")
            {
                var expr = ParseExpression(node.ChildNodes[1]);
                var opNode = node.ChildNodes[0].ChildNodes[0];
                var op = UnaryOp.Invert;
                switch (opNode.Token.ValueString)
                {
                    case "not":
                        op = UnaryOp.Negate;
                        break;
                    case "-":
                        op = UnaryOp.Invert;
                        break;
                    case "#":
                        op = UnaryOp.Length;
                        break;
                }
                return new UnaryExpression
                {
                    Expression = expr,
                    Operation = op
                };
            }
            throw new Exception("Invalid UnaryExpr node");
        }

        private FunctionDefinition ParseFunctionDef(ParseTreeNode node)
        {
            if (node.Term.Name == "FunctionDef")
            {
                var argsNode = node.ChildNodes[1].ChildNodes[0];
                var chunkNode = node.ChildNodes[2];

                var block = ParseBlock(chunkNode);
                var def = new FunctionDefinition();
                def.Body = block;
                def.Arguments = new List<Argument>();

                if (argsNode.ChildNodes.Count == 0)
                    return def;
                if (argsNode.ChildNodes.Count > 0)
                {
                    argsNode = argsNode.ChildNodes[0];
                    while (argsNode.ChildNodes.Count > 0)
                    {
                        var ident = argsNode.ChildNodes[0].Token.ValueString;
                        def.Arguments.Add(new Argument { Name = ident });
                        if (argsNode.ChildNodes.Count == 1)
                            break;
                        argsNode = argsNode.ChildNodes[1];
                    }
                }
                return def;
            }
            throw new Exception("Invalid FunctionDef node");
        }

        private IExpression ParsePrefix(ParseTreeNode node)
        {
            if (node.Term != null)
            {
                if (node.Term.Name == "Prefix")
                {
                    var child = node.ChildNodes[0];
                    if (child.Term.Name == "Variable")
                    {
                        return ParseVariable(child);
                    }
                    if (child.Term.Name == "Expression")
                    {
                        return ParseExpression(child);
                    }
                    if (child.Term.Name == "FunctionCall")
                    {
                        return ParseFunctionCall(child);
                    }
                    if (child.Term.Name == "OopCall")
                    {
                        return ParseOopCall(child);
                    }
                }
            }
            throw new Exception("Invalid Prefix node");
        }

        private IAssignable ParseVariable(ParseTreeNode node)
        {
            if (node.Term != null)
            {
                if (node.ChildNodes.Count == 1)
                {
                    var name = node.ChildNodes[0].Token.ValueString;
                    return new Variable { Name = name };
                }
                var prefix = ParsePrefix(node.ChildNodes[0]);
                if (node.ChildNodes[1].Term.Name == "Expression")
                {
                    var index = ParseExpression(node.ChildNodes[1]);
                    return new TableAccess { Expression = prefix, Index = index };
                }
                var name2 = node.ChildNodes[1].Token.ValueString;
                return new Variable { Name = name2, Prefix = prefix };
            }
            throw new Exception("Invalid Variable node");
        }

        private TableConstructor ParseTableConstruct(ParseTreeNode node)
        {
            if (node.Term.Name == "TableConstruct")
            {
                if (node.ChildNodes.Count == 0)
                {
                    return new TableConstructor { Values = new Dictionary<IExpression, IExpression>() };
                }
                var child = node.ChildNodes[0];
                var t = new TableConstructor();
                t.Values = new Dictionary<IExpression, IExpression>();

                var i = 1;
                while (true)
                {
                    if (child.ChildNodes.Count == 0)
                        break;

                    var value = child.ChildNodes[0];

                    if (value.ChildNodes.Count == 1)
                    {
                        t.Values.Add(new NumberLiteral { Value = i }, ParseExpression(value.ChildNodes[0]));
                        i++;
                    }
                    else
                    {
                        var prefix = value.ChildNodes[0];

                        IExpression key;
                        if (prefix.ChildNodes[0].Term.Name == "identifier")
                            key = new StringLiteral { Value = prefix.ChildNodes[0].Token.ValueString };
                        else
                            key = ParseExpression(prefix.ChildNodes[0]);

                        var expr = value.ChildNodes[1];
                        var val = ParseExpression(expr);

                        t.Values.Add(key, val);
                    }

                    //child = child.ChildNodes[1].ChildNodes[0];
                    child = child.ChildNodes[1];
                    if (child.ChildNodes.Count == 0)
                        break;
                    child = child.ChildNodes[0];
                }
                return t;
            }
            throw new Exception("Invalid TableConstruct node");
        }

        private IExpression ParseExpression(ParseTreeNode node)
        {
            if (node.Term.Name == "Expression")
            {
                var child = node.ChildNodes[0];
                if (child.Token != null && child.Token.Terminal is Irony.Parsing.NumberLiteral)
                {
                    return new NumberLiteral
                    {
                        Value = (child.Token.Value is double ? (double)(child.Token.Value) : (int)(child.Token.Value))
                    };
                }
                if (child.Token != null && child.Token.Terminal is Irony.Parsing.StringLiteral)
                {
                    return new StringLiteral { Value = (string)(child.Token.Value) };
                }
                if (child.Token != null && child.Token.Terminal is KeyTerm)
                {
                    var val = child.Token.ValueString;
                    if (val == "true")
                        return new BoolLiteral { Value = true };
                    if (val == "false")
                        return new BoolLiteral { Value = false };
                    if (val == "nil")
                        return new NilLiteral();
                }
                else if (child.Term != null && child.Term.Name == "Prefix")
                {
                    return ParsePrefix(child);
                }
                else if (child.Term != null && child.Term.Name == "OrOp")
                {
                    return ParseOrOp(child);
                }
                else if (child.Term != null && child.Term.Name == "FunctionDef")
                {
                    return ParseFunctionDef(child);
                }
                else if (child.Term != null && child.Term.Name == "UnaryExpr")
                {
                    return ParseUnaryExpr(child);
                }
                else if (child.Term != null && child.Term.Name == "TableConstruct")
                {
                    return ParseTableConstruct(child);
                }
                else if (child.Term != null && child.Term.Name == "OopCall")
                {
                    return ParseOopCall(child);
                }
                else if (child.Term != null && child.Term.Name == "Varargs")
                {
                    return new VarargsLiteral();
                }
            }
            throw new Exception("Invalid Expression node");
        }

        #endregion
    }
}
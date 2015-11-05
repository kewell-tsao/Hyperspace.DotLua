using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using DotLua.Ast;
using BinaryExpression = DotLua.Ast.BinaryExpression;
using UnaryExpression = DotLua.Ast.UnaryExpression;
#if DNXCORE50
using System.Linq;
#endif

namespace DotLua
{
    public static class LuaCompiler
    {
        private static readonly Type LuaContext_Type = typeof(LuaContext);
        private static readonly Type LuaArguments_Type = typeof(LuaArguments);
        private static readonly Type LuaEvents_Type = typeof(LuaEvents);
        private static readonly Type LuaObject_Type = typeof(LuaObject);
        private static readonly MethodInfo LuaContext_Get = LuaContext_Type.GetMethod("Get");
        private static readonly MethodInfo LuaContext_SetLocal = LuaContext_Type.GetMethod("SetLocal");
        private static MethodInfo LuaContext_SetGlobal = LuaContext_Type.GetMethod("SetGlobal");
        private static readonly MethodInfo LuaContext_Set = LuaContext_Type.GetMethod("Set");
        private static readonly MethodInfo LuaArguments_Concat = LuaArguments_Type.GetMethod("Concat");
        private static readonly MethodInfo LuaArguments_Add = LuaArguments_Type.GetMethod("Add");

        private static readonly ConstructorInfo LuaContext_New_parent =
            LuaContext_Type.GetConstructor(new[] { typeof(LuaContext) });

        private static readonly ConstructorInfo LuaArguments_New =
            LuaArguments_Type.GetConstructor(new[] { typeof(LuaObject[]) });

        private static ConstructorInfo LuaArguments_New_arglist =
            LuaArguments_Type.GetConstructor(new[] { typeof(LuaArguments[]) });

        private static readonly ConstructorInfo LuaArguments_New_void = LuaArguments_Type.GetConstructor(new Type[] { });

        private static readonly MethodInfo LuaEvents_eq = LuaEvents_Type.GetMethod("eq_event",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly MethodInfo LuaEvents_concat = LuaEvents_Type.GetMethod("concat_event",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly MethodInfo LuaEvents_len = LuaEvents_Type.GetMethod("len_event",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly MethodInfo LuaEvents_toNumber = LuaEvents_Type.GetMethod("toNumber",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly MethodInfo LuaObject_Call = LuaObject_Type.GetMethod("Call", new[] { LuaArguments_Type });
        private static readonly MethodInfo LuaObject_AsBool = LuaObject_Type.GetMethod("AsBool");
        private static readonly LuaArguments VoidArguments = new LuaArguments();

        #region Helpers

        private static Expression ToNumber(Expression Expression)
        {
            return Expression.Call(LuaEvents_toNumber, Expression);
        }

        private static Expression CreateLuaArguments(params Expression[] Expressions)
        {
            var array = Expression.NewArrayInit(typeof(LuaObject), Expressions);
            return Expression.New(LuaArguments_New, array);
        }

        private static Expression GetFirstArgument(Expression Expression)
        {
            if (Expression.Type == typeof(LuaArguments))
                return Expression.Property(Expression, "Item", Expression.Constant(0));
            return Expression.ArrayAccess(Expression, Expression.Constant(0));
        }

        private static Expression GetFirstArgumentAsBool(Expression Expression)
        {
            var e = GetFirstArgument(Expression);
            return Expression.Call(e, LuaObject_AsBool);
        }

        private static Expression GetAsBool(Expression Expression)
        {
            return Expression.Call(Expression, LuaObject_AsBool);
        }

        private static Expression GetArgument(Expression Expression, int n)
        {
            if (Expression.Type == typeof(LuaArguments))
                return Expression.Property(Expression, "Item", Expression.Constant(n));
            return Expression.ArrayAccess(Expression, Expression.Constant(n));
        }

        #endregion

        #region Expressions

        private static Expression CompileBinaryExpression(BinaryExpression expr, Expression Context)
        {
            var left = CompileSingleExpression(expr.Left, Context);
            var right = CompileSingleExpression(expr.Right, Context);
            switch (expr.Operation)
            {
                case BinaryOp.Addition:
                    return Expression.Add(left, right);
                case BinaryOp.And:
                    return Expression.And(left, right);
                case BinaryOp.Concat:
                    return Expression.Call(LuaEvents_concat, left, right);
                case BinaryOp.Different:
                    return Expression.Negate(Expression.Call(LuaEvents_eq, left, right));
                case BinaryOp.Division:
                    return Expression.Divide(left, right);
                case BinaryOp.Equal:
                    return Expression.Call(LuaEvents_eq, left, right);
                case BinaryOp.GreaterOrEqual:
                    return Expression.GreaterThanOrEqual(left, right);
                case BinaryOp.GreaterThan:
                    return Expression.GreaterThan(left, right);
                case BinaryOp.LessOrEqual:
                    return Expression.LessThanOrEqual(left, right);
                case BinaryOp.LessThan:
                    return Expression.LessThan(left, right);
                case BinaryOp.Modulo:
                    return Expression.Modulo(left, right);
                case BinaryOp.Multiplication:
                    return Expression.Multiply(left, right);
                case BinaryOp.Or:
                    return Expression.Or(left, right);
                case BinaryOp.Power:
                    return Expression.ExclusiveOr(left, right);
                case BinaryOp.Subtraction:
                    return Expression.Subtract(left, right);
                default:
                    throw new NotImplementedException();
            }
        }

        private static Expression CompileUnaryExpression(UnaryExpression expr, Expression Context)
        {
            var e = CompileSingleExpression(expr.Expression, Context);
            switch (expr.Operation)
            {
                case UnaryOp.Invert:
                    return Expression.Negate(e);
                case UnaryOp.Negate:
                    return Expression.Not(e);
                case UnaryOp.Length:
                    return Expression.Call(LuaEvents_len, e);
                default:
                    throw new NotImplementedException();
            }
        }

        private static Expression GetVariable(Variable expr, Expression Context)
        {
            return Expression.Call(Context, LuaContext_Get, Expression.Constant(expr.Name));
            //if (expr.Prefix == null)
            //    return Expression.Call(Context, LuaContext_Get, Expression.Constant(expr.Name));
            //var p = CompileSingleExpression(expr.Prefix, Context);
            //return Expression.Property(p, "Item", Expression.Convert(Expression.Constant(expr.Name), LuaObject_Type));
        }

        private static Expression GetTableAccess(TableAccess expr, Expression Context)
        {
            var e = CompileSingleExpression(expr.Expression, Context);
            var i = CompileSingleExpression(expr.Index, Context);

            return Expression.Property(e, "Item", i);
        }

        // This function returns a Expression with type LuaArguments. Similar to CompileSingleExpression
        private static Expression CompileExpression(IExpression expr, Expression Context)
        {
            if (expr is NumberLiteral)
                return CreateLuaArguments(Expression.Constant((LuaObject)((expr as NumberLiteral).Value)));
            if (expr is StringLiteral)
                return CreateLuaArguments(Expression.Constant((LuaObject)((expr as StringLiteral).Value)));
            if (expr is BoolLiteral)
                return CreateLuaArguments(Expression.Constant((LuaObject)((expr as BoolLiteral).Value)));
            if (expr is NilLiteral)
                return CreateLuaArguments(Expression.Constant(LuaObject.Nil));

            if (expr is BinaryExpression)
                return CreateLuaArguments(CompileBinaryExpression(expr as BinaryExpression, Context));
            if (expr is UnaryExpression)
                return CreateLuaArguments(CompileUnaryExpression(expr as UnaryExpression, Context));
            if (expr is Variable)
                return CreateLuaArguments(GetVariable(expr as Variable, Context));
            if (expr is TableAccess)
                return CreateLuaArguments(GetTableAccess(expr as TableAccess, Context));
            if (expr is FunctionCall)
                return CompileFunctionCallExpr(expr as FunctionCall, Context);
            if (expr is FunctionDefinition)
                return CreateLuaArguments(CompileFunctionDef(expr as FunctionDefinition, Context));
            if (expr is VarargsLiteral)
                return CompileVarargs(Context);
            if (expr is TableConstructor)
                return CreateLuaArguments(CompileTableConstructor(expr as TableConstructor, Context));
            throw new NotImplementedException();
        }

        // This function returns a Expression with type LuaObject. Similar to CompileExpression
        private static Expression CompileSingleExpression(IExpression expr, Expression Context)
        {
            if (expr is NumberLiteral)
                return (Expression.Constant((LuaObject)((expr as NumberLiteral).Value)));
            if (expr is StringLiteral)
                return (Expression.Constant((LuaObject)((expr as StringLiteral).Value)));
            if (expr is BoolLiteral)
                return (Expression.Constant((LuaObject)((expr as BoolLiteral).Value)));
            if (expr is NilLiteral)
                return (Expression.Constant(LuaObject.Nil));

            if (expr is BinaryExpression)
                return (CompileBinaryExpression(expr as BinaryExpression, Context));
            if (expr is UnaryExpression)
                return (CompileUnaryExpression(expr as UnaryExpression, Context));
            if (expr is Variable)
                return (GetVariable(expr as Variable, Context));
            if (expr is TableAccess)
                return (GetTableAccess(expr as TableAccess, Context));
            if (expr is FunctionCall)
                return GetFirstArgument(CompileFunctionCallExpr(expr as FunctionCall, Context));
            if (expr is FunctionDefinition)
                return (CompileFunctionDef(expr as FunctionDefinition, Context));
            if (expr is VarargsLiteral)
                return GetFirstArgument(CompileVarargs(Context));
            if (expr is TableConstructor)
                return (CompileTableConstructor(expr as TableConstructor, Context));
            throw new NotImplementedException();
        }

        private static Expression CompileTableConstructor(TableConstructor table, Expression Context)
        {
            var values = new List<KeyValuePair<Expression, Expression>>();
            var i = 0;
            var exprs = new List<Expression>();
            var type = typeof(Dictionary<LuaObject, LuaObject>);
            var add = type.GetMethod("Add", new[] { LuaObject_Type, LuaObject_Type });
            var variable = Expression.Parameter(type);
            var assign = Expression.Assign(variable, Expression.New(type.GetConstructor(new Type[] { })));
            exprs.Add(assign);
            foreach (var kvpair in table.Values)
            {
                if (i == table.Values.Count - 1)
                {
                    var k = CompileSingleExpression(kvpair.Key, Context);
                    var v = CompileExpression(kvpair.Value, Context);
                    var singlev = GetFirstArgument(v);
                    var ifFalse = Expression.Call(variable, add, k, singlev);

                    var counter = Expression.Parameter(typeof(int));
                    var value = Expression.Parameter(LuaArguments_Type);
                    var @break = Expression.Label();
                    var breakLabel = Expression.Label(@break);
                    var assignValue = Expression.Assign(value, v);
                    var assignCounter = Expression.Assign(counter, Expression.Constant(0));
                    var incrementCounter = Expression.Assign(counter, Expression.Increment(counter));
                    var loopCondition = Expression.LessThan(counter, Expression.Property(v, "Length"));
                    var addValue = Expression.Call(variable, add,
                        Expression.Add(k,
                            Expression.Call(LuaObject_Type.GetMethod("FromNumber"),
                                Expression.Convert(counter, typeof(double)))),
                        Expression.Property(value, "Item", counter));

                    var check = Expression.IfThenElse(loopCondition, Expression.Block(addValue, incrementCounter),
                        Expression.Break(@break));
                    var loopBody = Expression.Loop(check);
                    var ifTrue = Expression.Block(new[] { counter, value }, assignCounter, assignValue, loopBody,
                        breakLabel);

                    var condition = Expression.IsTrue(Expression.Property(k, "IsNumber"));
                    var ifblock = Expression.IfThenElse(condition, ifTrue, ifFalse);
                    exprs.Add(ifblock);
                }
                else
                {
                    var k = CompileSingleExpression(kvpair.Key, Context);
                    var v = CompileSingleExpression(kvpair.Value, Context);

                    exprs.Add(Expression.Call(variable, add, k, v));
                }
                i++;
            }
            exprs.Add(Expression.Call(LuaObject_Type.GetMethod("FromTable"), variable));
            var block = Expression.Block(new[] { variable }, exprs.ToArray());
            return Expression.Invoke(Expression.Lambda<Func<LuaObject>>(block));
        }

        private static Expression CompileVarargs(Expression Context)
        {
            return Expression.Property(Context, "Varargs");
        }

        private static Expression CompileFunctionDef(FunctionDefinition def, Expression Context)
        {
            return Expression.Invoke(CompileFunction(def, Context));
        }

        private static Expression CompileFunctionCallExpr(FunctionCall expr, Expression Context)
        {
            var function = CompileSingleExpression(expr.Function, Context);
            var args = new List<Expression>();
            Expression lastArg = null;
            var i = 0;
            foreach (var e in expr.Arguments)
            {
                if (i == expr.Arguments.Count - 1)
                    lastArg = CompileExpression(e, Context);
                else
                    args.Add(CompileSingleExpression(e, Context));
                i++;
            }
            var arg = Expression.NewArrayInit(LuaObject_Type, args.ToArray());
            var luaarg = Expression.New(LuaArguments_New, arg);

            if (lastArg == null)
                return Expression.Call(function, LuaObject_Call, luaarg);
            return Expression.Call(function, LuaObject_Call, Expression.Call(luaarg, LuaArguments_Concat, lastArg));
        }

        public static Expression<Func<LuaObject>> CompileFunction(FunctionDefinition func, Expression Context)
        {
            var exprs = new List<Expression>();

            var args = Expression.Parameter(LuaArguments_Type, "args");
            var label = Expression.Label(LuaArguments_Type, "exit");
            var @break = Expression.Label("break");

            var scopeVar = Expression.Parameter(LuaContext_Type, "funcScope");
            var assignScope = Expression.Assign(scopeVar, Expression.New(LuaContext_New_parent, Context));

            #region Arguments init

            var len = Expression.Property(args, "Length");
            var argLen = Expression.Constant(func.Arguments.Count);
            var argCount = Expression.Constant(func.Arguments.Count);

            var i = Expression.Parameter(typeof(int), "i");
            var assignI = Expression.Assign(i, Expression.Constant(0));
            var names = Expression.Parameter(typeof(string[]), "names");
#if DNXCORE50
            var assignNames = Expression.Assign(names, Expression.Constant(func.Arguments.Select(x => x.Name).ToArray()));
#else
            var assignNames = Expression.Assign(names,
                Expression.Constant(Array.ConvertAll(func.Arguments.ToArray(), x => x.Name)));
#endif

            var innerCond = Expression.LessThan(i, argLen);
            var outerCond = Expression.LessThan(i, len);

            var innerIf = Expression.Call(scopeVar, LuaContext_SetLocal, Expression.ArrayAccess(names, i),
                Expression.Property(args, "Item", i));
            var varargs = Expression.Property(scopeVar, "Varargs");
            var innerElse = Expression.Call(varargs, LuaArguments_Add, Expression.Property(args, "Item", i));

            var outerIf = Expression.Block(Expression.IfThenElse(innerCond, innerIf, innerElse),
                Expression.Assign(i, Expression.Add(i, Expression.Constant(1))));
            var outerElse = Expression.Break(@break);

            var loopBody = Expression.IfThenElse(outerCond, outerIf, outerElse);
            var loop = Expression.Loop(loopBody);

            var breakLabel = Expression.Label(@break);

            #endregion

            var body = CompileBlock(func.Body, label, null, scopeVar);

            exprs.Add(assignScope);
            exprs.Add(assignI);
            exprs.Add(assignNames);
            exprs.Add(loop);
            exprs.Add(breakLabel);
            exprs.Add(body);
            exprs.Add(Expression.Label(label, Expression.Constant(VoidArguments)));

            var funcBody = Expression.Block(new[] { i, names, scopeVar }, exprs.ToArray());

            var function = Expression.Lambda<LuaFunction>(funcBody, args);
            var returnValue = Expression.Lambda<Func<LuaObject>>(Expression.Convert(function, LuaObject_Type), null);

            return returnValue;
        }

        #endregion

        #region Statements

        private static Expression SetVariable(Variable expr, Expression value, Expression Context)
        {
            return Expression.Call(Context, LuaContext_Set, Expression.Constant(expr.Name), value);
            //if (expr.Prefix == null)
            //    return Expression.Call(Context, LuaContext_Set, Expression.Constant(expr.Name), value);
            //var prefix = CompileSingleExpression(expr.Prefix, Context);
            //var index = Expression.Constant((LuaObject)(expr.Name));
            //var set = Expression.Property(prefix, "Item", index);
            //return Expression.Assign(set, value);
        }

        private static Expression CompileAssignment(Assignment assign, Expression Context)
        {
            var variable = Expression.Parameter(typeof(LuaArguments), "vars");
            var stats = new List<Expression>();

            stats.Add(Expression.Assign(variable, Expression.New(LuaArguments_New_void)));
            foreach (var expr in assign.Expressions)
            {
                var ret = CompileExpression(expr, Context);
                stats.Add(Expression.Call(variable, LuaArguments_Concat, ret));
            }
            var i = 0;
            foreach (var var in assign.Variables)
            {
                var arg = GetArgument(variable, i);
                if (var is Variable)
                {
                    var x = var as Variable;
                    stats.Add(SetVariable(x, arg, Context));
                }
                else if (var is TableAccess)
                {
                    var x = var as TableAccess;

                    var expression = CompileSingleExpression(x.Expression, Context);
                    var index = CompileSingleExpression(x.Index, Context);

                    var set = Expression.Property(expression, "Item", index);
                    stats.Add(Expression.Assign(set, arg));
                }
                i++;
            }

            return Expression.Block(new[] { variable }, stats.ToArray());
        }

        private static Expression CompileLocalAssignment(LocalAssignment assign, Expression Context)
        {
            var variable = Expression.Parameter(typeof(LuaArguments), "vars");
            var stats = new List<Expression>();

            stats.Add(Expression.Assign(variable, Expression.New(LuaArguments_New_void)));
            foreach (var expr in assign.Values)
            {
                var ret = CompileExpression(expr, Context);
                stats.Add(Expression.Call(variable, LuaArguments_Concat, ret));
            }
            var i = 0;
            foreach (var var in assign.Names)
            {
                var arg = GetArgument(variable, i);
                stats.Add(Expression.Call(Context, LuaContext_SetLocal, Expression.Constant(var), arg));
                i++;
            }

            return Expression.Block(new[] { variable }, stats.ToArray());
        }

        private static Expression CompileFunctionCallStat(FunctionCall call, Expression Context)
        {
            var variable = Expression.Parameter(typeof(LuaArguments), "vars");
            var stats = new List<Expression>();

            stats.Add(Expression.Assign(variable, Expression.New(LuaArguments_New_void)));

            var expression = CompileSingleExpression(call.Function, Context);
            var i = 0;

            foreach (var arg in call.Arguments)
            {
                if (i == call.Arguments.Count - 1)
                {
                    var exp = CompileExpression(arg, Context);
                    stats.Add(Expression.Call(variable, LuaArguments_Concat, exp));
                }
                else
                {
                    var exp = CompileSingleExpression(arg, Context);
                    stats.Add(Expression.Call(variable, LuaArguments_Add, exp));
                }
                i++;
            }
            stats.Add(Expression.Call(expression, LuaObject_Call, variable));

            return Expression.Block(new[] { variable }, stats.ToArray());
        }

        private static Expression CompileBlock(Block block, LabelTarget returnTarget, LabelTarget breakTarget,
            Expression Context)
        {
            var exprs = new List<Expression>();
            var scope = Expression.Parameter(LuaContext_Type);
            exprs.Add(Expression.Assign(scope, Context));

            foreach (var s in block.Statements)
            {
                exprs.Add(CompileStatement(s, returnTarget, breakTarget, scope));
            }

            return Expression.Block(new[] { scope }, exprs.ToArray());
        }

        private static Expression CompileWhileStat(WhileStat stat, LabelTarget returnTarget, Expression Context)
        {
            var cond = GetAsBool(CompileSingleExpression(stat.Condition, Context));
            var breakTarget = Expression.Label("break");
            var loopBody = CompileBlock(stat.Block, returnTarget, breakTarget,
                Expression.New(LuaContext_New_parent, Context));
            var condition = Expression.IfThenElse(cond, loopBody, Expression.Break(breakTarget));
            var loop = Expression.Loop(condition);

            return Expression.Block(loop, Expression.Label(breakTarget));
        }

        private static Expression CompileIfStat(IfStat ifstat, LabelTarget returnTarget, LabelTarget breakTarget,
            Expression Context)
        {
            var condition = GetAsBool(CompileSingleExpression(ifstat.Condition, Context));
            var block = CompileBlock(ifstat.Block, returnTarget, breakTarget,
                Expression.New(LuaContext_New_parent, Context));

            if (ifstat.ElseIfs.Count == 0)
            {
                if (ifstat.ElseBlock == null)
                    return Expression.IfThen(condition, block);
                var elseBlock = CompileBlock(ifstat.ElseBlock, returnTarget, breakTarget,
                    Expression.New(LuaContext_New_parent, Context));
                return Expression.IfThenElse(condition, block, elseBlock);
            }
            Expression b = null;
            for (var i = ifstat.ElseIfs.Count - 1; i >= 0; i--)
            {
                var branch = ifstat.ElseIfs[i];
                var cond = GetAsBool(CompileSingleExpression(branch.Condition, Context));
                var body = CompileBlock(branch.Block, returnTarget, breakTarget,
                    Expression.New(LuaContext_New_parent, Context));
                if (b == null)
                {
                    if (ifstat.ElseBlock == null)
                        b = Expression.IfThen(cond, body);
                    else
                    {
                        var elseBlock = CompileBlock(ifstat.ElseBlock, returnTarget, breakTarget,
                            Expression.New(LuaContext_New_parent, Context));
                        b = Expression.IfThenElse(cond, body, elseBlock);
                    }
                }
                else
                    b = Expression.IfThenElse(cond, body, b);
            }

            var tree = Expression.IfThenElse(condition, block, b);
            return tree;
        }

        private static Expression CompileReturnStat(ReturnStat ret, LabelTarget returnTarget, Expression Context)
        {
            var variable = Expression.Parameter(LuaArguments_Type);
            var body = new List<Expression>();
            var ctor = Expression.New(LuaArguments_New_void);
            body.Add(Expression.Assign(variable, ctor));

            var i = 0;
            foreach (var expr in ret.Expressions)
            {
                if (i == ret.Expressions.Count - 1)
                {
                    var exp = CompileExpression(expr, Context);
                    body.Add(Expression.Call(variable, LuaArguments_Concat, exp));
                }
                else
                {
                    var exp = CompileSingleExpression(expr, Context);
                    body.Add(Expression.Call(variable, LuaArguments_Add, exp));
                }
            }

            body.Add(Expression.Return(returnTarget, variable));

            return Expression.Block(new[] { variable }, body.ToArray());
        }

        private static Expression CompileRepeatStatement(RepeatStat stat, LabelTarget returnTarget, Expression Context)
        {
            var ctx = Expression.New(LuaContext_New_parent, Context);
            var scope = Expression.Parameter(LuaContext_Type);
            var assignScope = Expression.Assign(scope, ctx);
            var @break = Expression.Label();

            var condition = GetAsBool(CompileSingleExpression(stat.Condition, scope));
            var body = CompileBlock(stat.Block, returnTarget, @break, scope);
            var check = Expression.IfThen(condition, Expression.Break(@break));
            var loop = Expression.Loop(Expression.Block(body, check));
            var block = Expression.Block(new[] { scope }, assignScope, loop, Expression.Label(@break));
            return block;
        }

        private static Expression CompileNumericFor(NumericFor stat, LabelTarget returnTarget, Expression Context)
        {
            var varValue = ToNumber(CompileSingleExpression(stat.Var, Context));
            var limit = ToNumber(CompileSingleExpression(stat.Limit, Context));
            var step = ToNumber(CompileSingleExpression(stat.Step, Context));

            var var = Expression.Parameter(LuaObject_Type);
            var scope = Expression.Parameter(LuaContext_Type);
            var @break = Expression.Label();
            var assignScope = Expression.Assign(scope, Expression.New(LuaContext_New_parent, Context));
            var assignVar = Expression.Assign(var, varValue);

            var condition =
                Expression.Or(
                    Expression.And(
                        Expression.GreaterThan(step, Expression.Constant((LuaObject)0)),
                        Expression.LessThanOrEqual(var, limit)),
                    Expression.And(
                        Expression.LessThanOrEqual(step, Expression.Constant((LuaObject)0)),
                        Expression.GreaterThanOrEqual(var, limit)));
            var setLocalVar = Expression.Call(scope, LuaContext_SetLocal, Expression.Constant(stat.Variable), var);
            var innerBlock = CompileBlock(stat.Block, returnTarget, @break, scope);
            var sum = Expression.Assign(var, Expression.Add(var, step));
            var check = Expression.IfThenElse(GetAsBool(condition), Expression.Block(setLocalVar, innerBlock, sum),
                Expression.Break(@break));
            var loop = Expression.Loop(check);

            var body = Expression.Block(new[] { var, scope }, assignScope, assignVar, loop, Expression.Label(@break));

            return body;
        }

        private static Expression CompileGenericFor(GenericFor stat, LabelTarget returnTarget, Expression Context)
        {
            var body = new List<Expression>();
            var args = Expression.Parameter(LuaArguments_Type);
            var f = GetArgument(args, 0);
            var s = GetArgument(args, 1);
            var var = GetArgument(args, 2);
            var fVar = Expression.Parameter(LuaObject_Type);
            var sVar = Expression.Parameter(LuaObject_Type);
            var varVar = Expression.Parameter(LuaObject_Type);
            var scope = Expression.Parameter(LuaContext_Type);

            var @break = Expression.Label();

            body.Add(Expression.Assign(args, Expression.New(LuaArguments_New_void)));
            foreach (var expr in stat.Expressions)
            {
                body.Add(Expression.Call(args, LuaArguments_Concat, CompileExpression(expr, Context)));
            }
            body.Add(Expression.Assign(fVar, f));
            body.Add(Expression.Assign(sVar, s));
            body.Add(Expression.Assign(varVar, var));

            body.Add(Expression.Assign(scope, Expression.New(LuaContext_New_parent, Context)));

            var res = Expression.Parameter(LuaArguments_Type);
            var buildArgs = Expression.New(LuaArguments_New, Expression.NewArrayInit(typeof(LuaObject), sVar, varVar));
            var resAssign = Expression.Assign(res, Expression.Call(fVar, LuaObject_Call, buildArgs));
            var exprs = new List<Expression>();
            exprs.Add(resAssign);
            for (var i = 0; i < stat.Variables.Count; i++)
            {
                var val = GetArgument(res, i);
                exprs.Add(Expression.Call(scope, LuaContext_SetLocal, Expression.Constant(stat.Variables[i]), val));
            }
            var check = Expression.IfThen(Expression.Property(GetArgument(res, 0), "IsNil"), Expression.Break(@break));
            exprs.Add(check);
            exprs.Add(Expression.Assign(varVar, GetFirstArgument(res)));
            exprs.Add(CompileBlock(stat.Block, returnTarget, @break, scope));

            var loopBody = Expression.Block(new[] { res }, exprs.ToArray());
            var loop = Expression.Loop(loopBody);
            body.Add(loop);
            body.Add(Expression.Label(@break));

            var block = Expression.Block(new[] { args, scope, fVar, sVar, varVar }, body.ToArray());

            return block;
        }

        private static Expression CompileStatement(IStatement stat, LabelTarget returnTarget, LabelTarget breakTarget,
            Expression Context)
        {
            if (stat is Assignment)
                return CompileAssignment(stat as Assignment, Context);
            if (stat is LocalAssignment)
                return CompileLocalAssignment(stat as LocalAssignment, Context);
            if (stat is FunctionCall)
                return CompileFunctionCallStat(stat as FunctionCall, Context);
            if (stat is Block)
                return CompileBlock(stat as Block, returnTarget, breakTarget, Context);
            if (stat is IfStat)
                return CompileIfStat(stat as IfStat, returnTarget, breakTarget, Context);
            if (stat is ReturnStat)
                return CompileReturnStat(stat as ReturnStat, returnTarget, Context);
            if (stat is BreakStat)
                return Expression.Break(breakTarget);
            if (stat is WhileStat)
                return CompileWhileStat(stat as WhileStat, returnTarget, Context);
            if (stat is RepeatStat)
                return CompileRepeatStatement(stat as RepeatStat, returnTarget, Context);
            if (stat is GenericFor)
                return CompileGenericFor(stat as GenericFor, returnTarget, Context);
            if (stat is NumericFor)
                return CompileNumericFor(stat as NumericFor, returnTarget, Context);
            throw new NotImplementedException();
        }

        #endregion
    }
}
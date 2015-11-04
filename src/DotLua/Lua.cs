using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using DotLua.Ast;

namespace DotLua
{
    public class Lua : DynamicObject
    {
        private readonly Parser _parser = new Parser();

        /// <summary>
        ///     Creates a new Lua context with the base functions already set
        /// </summary>
        public Lua()
        {
            Context.Set("assert", (LuaFunction) assert);
            Context.Set("dofile", (LuaFunction) dofile);
            Context.Set("error", (LuaFunction) error);
            Context.Set("getmetatable", (LuaFunction) getmetatable);
            Context.Set("setmetatable", (LuaFunction) setmetatable);
            Context.Set("rawequal", (LuaFunction) rawequal);
            Context.Set("rawget", (LuaFunction) rawget);
            Context.Set("rawset", (LuaFunction) rawset);
            Context.Set("rawlen", (LuaFunction) rawlen);
            Context.Set("tonumber", (LuaFunction) tonumber);
            Context.Set("tostring", (LuaFunction) tostring);
            Context.Set("type", (LuaFunction) type);
            Context.Set("ipairs", (LuaFunction) ipairs);
            Context.Set("next", (LuaFunction) next);
            Context.Set("pairs", (LuaFunction) pairs);
        }

        /// <summary>
        ///     The base context
        /// </summary>
        public LuaContext Context { get; } = new LuaContext();

        /// <summary>
        ///     The base context
        /// </summary>
        public dynamic DynamicContext
        {
            get { return Context; }
        }

        /// <summary>
        ///     Helper function for returning Nil from a function
        /// </summary>
        /// <returns>Nil</returns>
        public static LuaArguments Return()
        {
            return new[] {LuaObject.Nil};
        }

        /// <summary>
        ///     Helper function for returning objects from a function
        /// </summary>
        /// <param name="values">The objects to return</param>
        public static LuaArguments Return(params LuaObject[] values)
        {
            return values;
        }

        /// <summary>
        ///     Parses and executes the specified file
        /// </summary>
        /// <param name="Filename">The file to execute</param>
        public LuaArguments DoFile(string Filename)
        {
            var def = new FunctionDefinition();
            def.Arguments = new List<Argument>();
            def.Body = _parser.ParseFile(Filename);
            var function = LuaCompiler.CompileFunction(def, Expression.Constant(Context)).Compile();
            return function().Call(Return());
        }

        /// <summary>
        ///     Parses and executes the specified string
        /// </summary>
        public LuaArguments DoString(string Chunk)
        {
            var def = new FunctionDefinition();
            def.Arguments = new List<Argument>();
            def.Body = _parser.ParseString(Chunk);
            var function = LuaCompiler.CompileFunction(def, Expression.Constant(Context)).Compile();
            return function().Call(Return());
        }

        #region Dynamic methods

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            var obj = Context.Get(binder.Name);
            if (obj.IsNil)
                return false;
            result = LuaObject.getObject(obj);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (value is LuaObject)
                Context.Set(binder.Name, value as LuaObject);
            else
                Context.Set(binder.Name, LuaObject.FromObject(value));
            return true;
        }

        #endregion

        #region Basic functions

        private LuaArguments assert(LuaArguments args)
        {
            if (args.Length > 0)
            {
                if (args[0].AsBool() == false)
                {
                    if (args.Length == 1)
                        throw new LuaException("Assertion failed");
                    throw new LuaException(args[1].ToString());
                }
            }
            return Return();
        }

        private LuaArguments dofile(LuaArguments args)
        {
            return DoFile(args[0].ToString());
        }

        private LuaArguments error(LuaArguments args)
        {
            throw new LuaException(args[0].ToString());
        }

        private LuaArguments getmetatable(LuaArguments args)
        {
            return Return(args[0].Metatable);
        }

        private LuaArguments setmetatable(LuaArguments args)
        {
            args[0].Metatable = args[1];
            return Return();
        }

        private LuaArguments rawequal(LuaArguments args)
        {
            return Return(args[0] == args[1]);
        }

        private LuaArguments rawget(LuaArguments args)
        {
            return Return(LuaEvents.rawget(args[0], args[1]));
        }

        private LuaArguments rawset(LuaArguments args)
        {
            LuaEvents.rawset(args[0], args[1], args[2]);
            return Return(args[0]);
        }

        private LuaArguments rawlen(LuaArguments args)
        {
            var obj = args[0];
            if (obj.IsString)
                return Return(obj.AsString().Length);
            if (obj.IsTable)
                return Return(obj.AsTable().Count);
            throw new LuaException("invalid argument");
        }

        private LuaArguments tonumber(LuaArguments args)
        {
            var obj = args[0];
            if (args.Length == 1)
            {
                double d = 0;
                if (obj.IsString)
                {
                    if (double.TryParse(obj.AsString(), out d))
                        return Return(d);
                    return Return();
                }
                if (obj.IsNumber)
                {
                    return Return(obj.AsNumber());
                }
                return Return();
            }
            //TODO: Implement tonumber for bases different from 10
            throw new NotImplementedException();
        }

        private LuaArguments tostring(LuaArguments args)
        {
            return Return(LuaEvents.tostring_event(args[0]));
        }

        private LuaArguments type(LuaArguments args)
        {
            switch (args[0].Type)
            {
                case LuaType.boolean:
                    return Return("boolean");
                case LuaType.function:
                    return Return("function");
                case LuaType.nil:
                    return Return("nil");
                case LuaType.number:
                    return Return("number");
                case LuaType.@string:
                    return Return("string");
                case LuaType.table:
                    return Return("table");
                case LuaType.thread:
                    return Return("thread");
                case LuaType.userdata:
                    return Return("userdata");
            }
            return Return();
        }

        private LuaArguments ipairs(LuaArguments args)
        {
            var handler = LuaEvents.getMetamethod(args[0], "__ipairs");
            if (!handler.IsNil)
            {
                return handler.Call(args);
            }
            if (args[0].IsTable)
            {
                LuaFunction f = delegate(LuaArguments x)
                {
                    var s = x[0];
                    var var = x[1].AsNumber() + 1;

                    var val = s[var];
                    if (val == LuaObject.Nil)
                        return Return(LuaObject.Nil);
                    return Return(var, val);
                };
                return Return(f, args[0], 0);
            }
            throw new LuaException("t must be a table");
        }

        private LuaArguments next(LuaArguments args)
        {
            var table = args[0];
            var index = args[1];
            if (!table.IsTable)
            {
                throw new LuaException("t must be a table");
            }
            var keys = new List<LuaObject>(table.AsTable().Keys);
            if (index.IsNil)
            {
                if (keys.Count == 0)
                    return Return();
                return Return(keys[0], table[keys[0]]);
            }
            var pos = keys.IndexOf(index);
            if (pos == keys.Count - 1)
            {
                return Return();
            }
            return Return(keys[pos + 1], table[keys[pos + 1]]);
        }

        private LuaArguments pairs(LuaArguments args)
        {
            var handler = LuaEvents.getMetamethod(args[0], "__pairs");
            if (!handler.IsNil)
            {
                return handler.Call(args);
            }
            return Return((LuaFunction) next, args[0], LuaObject.Nil);
        }

        #endregion
    }
}
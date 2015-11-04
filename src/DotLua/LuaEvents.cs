// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;

namespace DotLua
{
    internal static class LuaEvents
    {
        // This is all based on the Lua 5.2 manual
        // http://www.lua.org/manual/5.2/manual.html#2.4

        public static LuaObject rawget(LuaObject table, LuaObject index)
        {
            if (table.IsTable)
            {
                LuaObject obj;
                if (table.AsTable().TryGetValue(index, out obj))
                    return obj;
                return LuaObject.Nil;
            }
            throw new LuaException("Invalid operation");
        }

        public static void rawset(LuaObject table, LuaObject key, LuaObject value)
        {
            if (table.IsTable)
            {
                var t = table.AsTable();
                if (t.ContainsKey(key))
                    t[key] = value;
                else
                    t.Add(key, value);
            }
            else
                throw new LuaException("Invalid operation");
        }

        private static LuaObject getBinhandler(LuaObject a, LuaObject b, string f)
        {
            var f1 = getMetamethod(a, f);
            var f2 = getMetamethod(b, f);

            return f1 | f2;
        }

        private static LuaObject getEqualhandler(LuaObject a, LuaObject b)
        {
            if ((a.Type != b.Type) || (a.IsTable && b.IsUserData))
                return LuaObject.Nil;
            var mm1 = getMetamethod(a, "__eq");
            var mm2 = getMetamethod(b, "__eq");
            if (mm1 == mm2)
                return mm1;
            return LuaObject.Nil;
        }

        internal static LuaObject toNumber(LuaObject obj)
        {
            if (obj.IsNumber)
                return LuaObject.FromNumber(obj.AsNumber());
            if (obj.IsString)
            {
                double d;
                if (double.TryParse(obj.AsString(), out d))
                    return LuaObject.FromNumber(d);
                return LuaObject.Nil;
            }
            return LuaObject.Nil;
        }

        internal static bool ConvertToNumber(LuaObject obj, out double value)
        {
            if (obj.IsNumber)
            {
                value = (double)obj.luaobj;
                return true;
            }
            if (obj.IsString)
            {
                if (double.TryParse(obj.AsString(), out value))
                    return true;
                return false;
            }
            value = 0d;
            return false;
        }

        internal static LuaObject getMetamethod(LuaObject obj, string e)
        {
            if (obj.Metatable == null || obj.Metatable.IsNil)
                return LuaObject.Nil;
            return obj.Metatable[e];
        }

        internal static LuaObject add_event(LuaObject op1, LuaObject op2)
        {
            double a, b;

            if (ConvertToNumber(op1, out a) && ConvertToNumber(op2, out b))
                return LuaObject.FromNumber(a + b);
            var handler = getBinhandler(op1, op2, "__add");
            if (!handler.IsNil)
                return handler.Call(op1, op2)[0];

            throw new LuaException("Invalid arithmetic operation");
        }

        internal static LuaObject sub_event(LuaObject op1, LuaObject op2)
        {
            double a, b;

            if (ConvertToNumber(op1, out a) && ConvertToNumber(op2, out b))
                return LuaObject.FromNumber(a - b);
            var handler = getBinhandler(op1, op2, "__sub");
            if (!handler.IsNil)
                return handler.Call(op1, op2)[0];

            throw new LuaException("Invalid arithmetic operation");
        }

        internal static LuaObject mul_event(LuaObject op1, LuaObject op2)
        {
            double a, b;

            if (ConvertToNumber(op1, out a) && ConvertToNumber(op2, out b))
                return LuaObject.FromNumber(a * b);
            var handler = getBinhandler(op1, op2, "__mul");
            if (!handler.IsNil)
                return handler.Call(op1, op2)[0];

            throw new LuaException("Invalid arithmetic operation");
        }

        internal static LuaObject div_event(LuaObject op1, LuaObject op2)
        {
            double a, b;

            if (ConvertToNumber(op1, out a) && ConvertToNumber(op2, out b))
                return LuaObject.FromNumber(a / b);
            var handler = getBinhandler(op1, op2, "__div");
            if (!handler.IsNil)
                return handler.Call(op1, op2)[0];

            throw new LuaException("Invalid arithmetic operation");
        }

        internal static LuaObject mod_event(LuaObject op1, LuaObject op2)
        {
            double a, b;

            if (ConvertToNumber(op1, out a) && ConvertToNumber(op2, out b))
                return LuaObject.FromNumber(a - Math.Floor(a / b) * b);
            var handler = getBinhandler(op1, op2, "__mod");
            if (!handler.IsNil)
                return handler.Call(op1, op2)[0];

            throw new LuaException("Invalid arithmetic operation");
        }

        internal static LuaObject pow_event(LuaObject op1, LuaObject op2)
        {
            double a, b;

            if (ConvertToNumber(op1, out a) && ConvertToNumber(op2, out b))
                return LuaObject.FromNumber(Math.Pow(a, b));
            var handler = getBinhandler(op1, op2, "__pow");
            if (!handler.IsNil)
                return handler.Call(op1, op2)[0];

            throw new LuaException("Invalid arithmetic operation");
        }

        internal static LuaObject unm_event(LuaObject op)
        {
            double a;

            if (ConvertToNumber(op, out a))
            {
                return LuaObject.FromNumber(-a);
            }
            var handler = getMetamethod(op, "__unm");
            if (!handler.IsNil)
                return handler.Call(op)[0];

            throw new LuaException("Invalid arithmetic operation");
        }

        internal static LuaObject index_event(LuaObject table, LuaObject key)
        {
            LuaObject handler;

            if (table.IsTable)
            {
                var v = rawget(table, key);
                if (!v.IsNil)
                    return v;
                handler = getMetamethod(table, "__index");
                if (handler.IsNil)
                    return LuaObject.Nil;
            }
            else
            {
                handler = getMetamethod(table, "__index");
                if (handler.IsNil)
                    throw new LuaException("Invalid argument");
            }

            if (handler.IsFunction)
                return handler.AsFunction()(new[] { table, key })[0];
            if (!handler.IsNil)
                return handler[key];
            return LuaObject.Nil;
        }

        internal static LuaObject newindex_event(LuaObject table, LuaObject key, LuaObject value)
        {
            LuaObject handler;
            if (table.IsTable)
            {
                var v = rawget(table, key);
                if (!v.IsNil)
                {
                    rawset(table, key, value);
                    return LuaObject.Nil;
                }
                handler = getMetamethod(table, "__newindex");
                if (handler.IsNil)
                {
                    rawset(table, key, value);
                    return LuaObject.Nil;
                }
            }
            else
            {
                handler = getMetamethod(table, "__newindex");
                if (handler.IsNil)
                    throw new LuaException("Invalid op");
            }

            if (handler.IsFunction)
                handler.AsFunction()(new[] { table, key, value });
            else
                handler[key] = value;
            return LuaObject.Nil;
        }

        internal static LuaArguments call_event(LuaObject func, LuaArguments args)
        {
            if (func.IsFunction)
                return func.AsFunction()(args);
            var handler = getMetamethod(func, "__call");
            if (handler.IsFunction)
            {
                var argslist = new List<LuaObject>();
                argslist.Add(func);
                argslist.AddRange(args);
                return handler.AsFunction()(argslist.ToArray());
            }
            throw new LuaException("Cannot call nonfunction");
        }

        internal static LuaObject len_event(LuaObject op)
        {
            if (op.IsString)
                return op.AsString().Length;
            var handler = getMetamethod(op, "__len");
            if (!handler.IsNil)
                return handler.Call(op)[0];
            if (op.IsTable)
                return op.AsTable().Count;
            throw new LuaException("Invalid op");
        }

        internal static LuaObject concat_event(LuaObject op1, LuaObject op2)
        {
            if ((op1.IsString || op1.IsNumber) && (op2.IsString || op2.IsNumber))
                return string.Concat(op1, op2);
            var handler = getBinhandler(op1, op2, "__concat");
            if (!handler.IsNil)
                return handler.Call(op1, op2)[0];
            throw new LuaException("Invalid op");
        }

        internal static LuaObject eq_event(LuaObject op1, LuaObject op2)
        {
            if (op1 == op2)
                return true;
            var handler = getEqualhandler(op1, op2);
            if (!handler.IsNil)
                return !(!(handler.Call(op1, op2)[0].AsBool()));
            return false;
        }

        internal static LuaObject lt_event(LuaObject op1, LuaObject op2)
        {
            if (op1.IsNumber && op2.IsNumber)
                return op1.AsNumber() < op2.AsNumber();
            if (op1.IsString && op2.IsString)
            {
                var n = StringComparer.CurrentCulture.Compare(op1.AsString(), op2.AsString());
                return (n < 0);
            }
            var handler = getBinhandler(op1, op2, "__lt");
            if (!handler.IsNil)
                return !(!(handler.Call(op1, op2)[0].AsBool()));
            throw new ArgumentException("attempt to compare " + op1.type + " with " + op2.type);
        }

        internal static LuaObject le_event(LuaObject op1, LuaObject op2)
        {
            if (op1.IsNumber && op2.IsNumber)
                return op1.AsNumber() <= op2.AsNumber();
            if (op1.IsString && op2.IsString)
            {
                var n = StringComparer.CurrentCulture.Compare(op1.AsString(), op2.AsString());
                return (n <= 0);
            }
            var handler = getBinhandler(op1, op2, "__le");
            if (!handler.IsNil)
                return !(!(handler.Call(op1, op2)[0].AsBool()));
            throw new ArgumentException("attempt to compare " + op1.type + " with " + op2.type);
        }

        internal static LuaObject gc_event(LuaObject op)
        {
            var handler = getMetamethod(op, "__gc");
            if (handler.IsFunction)
                handler.AsFunction()(new[] { op });
            return LuaObject.Nil;
        }

        internal static LuaObject tostring_event(LuaObject op)
        {
            var handler = getMetamethod(op, "__tostring");
            if (!handler.IsNil)
                return handler.Call(op)[0];
            return op.ToString();
        }
    }
}
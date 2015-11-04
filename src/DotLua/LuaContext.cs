using System.Collections.Generic;
using System.Dynamic;

namespace DotLua
{
    /// <summary>
    ///     Holds a scope and its variables
    /// </summary>
    public class LuaContext : DynamicObject
    {
        private readonly LuaContext parent;
        private readonly Dictionary<string, LuaObject> variables;

        /// <summary>
        ///     Used to create scopes
        /// </summary>
        public LuaContext(LuaContext Parent)
        {
            parent = Parent;
            variables = new Dictionary<string, LuaObject>();
            Varargs = new LuaArguments(new LuaObject[] {});
        }

        /// <summary>
        ///     Creates a base context
        /// </summary>
        public LuaContext() : this(null)
        {
        }

        internal LuaArguments Varargs { get; set; }

        /// <summary>
        ///     Sets or creates a variable in the local scope
        /// </summary>
        public void SetLocal(string Name, LuaObject Value)
        {
            variables[Name] = Value;
        }

        /// <summary>
        ///     Sets or creates a variable in the global scope
        /// </summary>
        public void SetGlobal(string Name, LuaObject Value)
        {
            if (parent == null)
                variables[Name] = Value;
            else
                parent.SetGlobal(Name, Value);
        }

        /// <summary>
        ///     Returns the nearest declared variable value or nil
        /// </summary>
        public LuaObject Get(string Name)
        {
            var obj = LuaObject.Nil;
            if (variables.TryGetValue(Name, out obj) || parent == null)
            {
                if (obj == null)
                    return LuaObject.Nil;
                return obj;
            }
            return parent.Get(Name);
        }

        /// <summary>
        ///     Sets the nearest declared variable or creates a new one
        /// </summary>
        public void Set(string Name, LuaObject Value)
        {
            var obj = LuaObject.Nil;
            if (parent == null || variables.TryGetValue(Name, out obj))
                variables[Name] = Value;
            else
                parent.Set(Name, Value);
        }

        #region DynamicObject

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = Get(binder.Name);
            if (result == LuaObject.Nil)
                return false;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Set(binder.Name, LuaObject.FromObject(value));
            return true;
        }

        #endregion
    }
}
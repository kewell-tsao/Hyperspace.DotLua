using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Irony.Interpreter.Ast;

namespace Irony.Interpreter
{

    //Unfinished, work in progress, file disabled for now

    public enum ClrTargetType
    {
        Namespace,
        Type,
        Method,
        Property,
        Field,
    }

    public class ClrInteropBindingTargetInfo : BindingTargetInfo, IBindingSource
    {
        public ClrTargetType TargetSubType;

        public ClrInteropBindingTargetInfo(string symbol, ClrTargetType targetSubType) : base(symbol, BindingTargetType.ClrInterop)
        {
            TargetSubType = targetSubType;
        }

        public virtual Binding Bind(BindingRequest request)
        {
            throw new NotImplementedException();
        }
    }//class

    public class ClrNamespaceBindingTargetInfo : ClrInteropBindingTargetInfo
    {
        ConstantBinding _binding;
        public ClrNamespaceBindingTargetInfo(string ns) : base(ns, ClrTargetType.Namespace)
        {
            _binding = new ConstantBinding(ns, this);
        }
        public override Binding Bind(BindingRequest request)
        {
            return _binding;
        }
    }

    public class ClrTypeBindingTargetInfo : ClrInteropBindingTargetInfo
    {
        ConstantBinding _binding;
        public ClrTypeBindingTargetInfo(Type type) : base(type.Name, ClrTargetType.Type)
        {
            _binding = new ConstantBinding(type, this);
        }
        public override Binding Bind(BindingRequest request)
        {
            return _binding;
        }
    }

    public class ClrMethodBindingTargetInfo : ClrInteropBindingTargetInfo, ICallTarget
    { //The object works as ICallTarget itself
        public object Instance;
        public Type DeclaringType;
        BindingFlags _invokeFlags;
        Binding _binding;

        public ClrMethodBindingTargetInfo(Type declaringType, string methodName, object instance = null) : base(methodName, ClrTargetType.Method)
        {
            DeclaringType = declaringType;
            Instance = instance;
#if DNXCORE50
            _invokeFlags = BindingFlags.Public | BindingFlags.NonPublic;
#else
            _invokeFlags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic;
#endif
            if (Instance == null)
                _invokeFlags |= BindingFlags.Static;
            else
                _invokeFlags |= BindingFlags.Instance;
            _binding = new ConstantBinding(target: this as ICallTarget, targetInfo: this);
            //The object works as CallTarget itself; the "as" conversion is not needed in fact, we do it just to underline the role
        }

        public override Binding Bind(BindingRequest request)
        {
            return _binding;
        }

        #region ICalllable.Call implementation
        public object Call(ScriptThread thread, object[] args)
        {
            // TODO: fix this. Currently doing it slow but easy way, through reflection
            if (args != null && args.Length == 0)
                args = null;
#if DNXCORE50
            var result = DeclaringType.GetMethod(base.Symbol, _invokeFlags).Invoke(Instance, args);
#else
            var result = DeclaringType.InvokeMember(base.Symbol, _invokeFlags, null, Instance, args);
#endif
            return result;
        }
        #endregion
    }

    public class ClrPropertyBindingTargetInfo : ClrInteropBindingTargetInfo
    {
        public object Instance;
        public PropertyInfo Property;
        Binding _binding;

        public ClrPropertyBindingTargetInfo(PropertyInfo property, object instance) : base(property.Name, ClrTargetType.Property)
        {
            Property = property;
            Instance = instance;
            _binding = new Binding(this);
            _binding.GetValueRef = GetPropertyValue;
            _binding.SetValueRef = SetPropertyValue;
        }
        public override Binding Bind(BindingRequest request)
        {
            return _binding;
        }
        private object GetPropertyValue(ScriptThread thread)
        {
            var result = Property.GetValue(Instance, null);
            return result;
        }
        private void SetPropertyValue(ScriptThread thread, object value)
        {
            Property.SetValue(Instance, value, null);
        }
    }

    public class ClrFieldBindingTargetInfo : ClrInteropBindingTargetInfo
    {
        public object Instance;
        public FieldInfo Field;
        Binding _binding;

        public ClrFieldBindingTargetInfo(FieldInfo field, object instance) : base(field.Name, ClrTargetType.Field)
        {
            Field = field;
            Instance = instance;
            _binding = new Binding(this);
            _binding.GetValueRef = GetPropertyValue;
            _binding.SetValueRef = SetPropertyValue;
        }
        public override Binding Bind(BindingRequest request)
        {
            return _binding;
        }
        private object GetPropertyValue(ScriptThread thread)
        {
            var result = Field.GetValue(Instance);
            return result;
        }
        private void SetPropertyValue(ScriptThread thread, object value)
        {
            Field.SetValue(Instance, value);
        }
    }

    // Method for adding methods to BuiltIns table in Runtime
    public static partial class BindingSourceTableExtensions
    {
        public static void ImportStaticMembers(this BindingSourceTable targets, Type fromType)
        {
            var members = fromType.GetMembers(BindingFlags.Public | BindingFlags.Static);
            foreach (var member in members)
            {
                if (targets.ContainsKey(member.Name)) continue; //do not import overloaded methods several times
                switch (member.GetMemberType())
                {
                    case MemberTypes.Method:
                        targets.Add(member.Name, new ClrMethodBindingTargetInfo(fromType, member.Name));
                        break;
                    case MemberTypes.Property:
                        targets.Add(member.Name, new ClrPropertyBindingTargetInfo(member as PropertyInfo, null));
                        break;
                    case MemberTypes.Field:
                        targets.Add(member.Name, new ClrFieldBindingTargetInfo(member as FieldInfo, null));
                        break;
                }//switch
            }//foreach
        }//method
    }


#if DNXCORE50

    [Flags]
    public enum MemberTypes
    {
        Constructor = 1,
        Event = 2,
        Field = 4,
        Method = 8,
        Property = 16,
        TypeInfo = 32,
        Custom = 64,
        NestedType = 128,
        All = 191
    }

#endif


    public static class MemberTypesExts
    {
        public static MemberTypes GetMemberType(this MemberInfo memberInfo)
        {
#if DNXCORE50
            if (memberInfo is MethodInfo)
                return MemberTypes.Method;
            if (memberInfo is PropertyInfo)
                return MemberTypes.Property;
            if (memberInfo is FieldInfo)
                return MemberTypes.Field;
            if (memberInfo is EventInfo)
                return MemberTypes.Event;
            if (memberInfo is TypeInfo)
                return MemberTypes.TypeInfo;
            if (memberInfo is ConstructorInfo)
                return MemberTypes.Constructor;
            throw new ArgumentException("Unknow member info type.", nameof(memberInfo));
#else
            return memberInfo.MemberType;
#endif
        }
    }


}

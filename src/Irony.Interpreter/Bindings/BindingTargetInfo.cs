using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Irony.Interpreter
{

    public enum BindingTargetType
    {
        Slot,
        BuiltInObject,
        SpecialForm,
        ClrInterop,
        Custom, // any special non-standard type for specific language
    }


    public class BindingTargetInfo
    {
        public readonly string Symbol;
        public readonly BindingTargetType Type;
        public BindingTargetInfo(string symbol, BindingTargetType type)
        {
            Symbol = symbol;
            Type = type;
        }

        public override string ToString()
        {
            return Symbol + "/" + Type.ToString();
        }

    }//class


}

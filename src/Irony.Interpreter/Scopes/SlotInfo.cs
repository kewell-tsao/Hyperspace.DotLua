using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Irony.Interpreter
{
    public enum SlotType
    {
        Value,     //local or property value
        Parameter, //function parameter
        Function,
        Closure,
    }

    /// <summary> Describes a variable. </summary>
    public class SlotInfo
    {
        public readonly ScopeInfo ScopeInfo;
        public readonly SlotType Type;
        public readonly string Name;
        public readonly int Index;
        public bool IsPublic = true; //for module-level slots, indicator that symbol is "exported" and visible by code that imports the module
        internal SlotInfo(ScopeInfo scopeInfo, SlotType type, string name, int index)
        {
            ScopeInfo = scopeInfo;
            Type = type;
            Name = name;
            Index = index;
        }
    }

    public class SlotInfoDictionary : Dictionary<string, SlotInfo>
    {
#if DNXCORE50
        public SlotInfoDictionary(bool caseSensitive)
            : base(32, CultureInfo.InvariantCulture.CompareInfo.GetStringComparer(caseSensitive ? CompareOptions.None : CompareOptions.IgnoreCase))
        {
        }
#else
        public SlotInfoDictionary(bool caseSensitive)
            : base(32, caseSensitive ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase)
        {

        }
#endif
    }

}

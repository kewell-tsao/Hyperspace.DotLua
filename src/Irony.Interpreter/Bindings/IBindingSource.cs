using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Irony.Interpreter
{

    public interface IBindingSource
    {
        Binding Bind(BindingRequest request);
    }

    public class BindingSourceList : List<IBindingSource>
    {
    }

    public class BindingSourceTable : Dictionary<string, IBindingSource>, IBindingSource
    {
#if DNXCORE50
        public BindingSourceTable(bool caseSensitive)
            : base(CultureInfo.InvariantCulture.CompareInfo.GetStringComparer(caseSensitive ? CompareOptions.None : CompareOptions.IgnoreCase))
        {
        }
#else
        public BindingSourceTable(bool caseSensitive)
            : base(caseSensitive ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase)
        {
        }
#endif

        //IBindingSource Members
        public Binding Bind(BindingRequest request)
        {
            IBindingSource target;
            if (TryGetValue(request.Symbol, out target))
                return target.Bind(request);
            return null;
        }
    }//class

    // This class will be used to define extensions for BindingSourceTable
    public static partial class BindingSourceTableExtensions
    {
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Irony.Interpreter
{

    // Module export, container for public, exported symbols from module
    // Just a skeleton, to be completed
    public class ModuleExport : IBindingSource
    {
        public ModuleInfo Module;
        public ModuleExport(ModuleInfo module)
        {
            Module = module;
        }

        public Binding Bind(BindingRequest request)
        {
            return null;
        }
    }



}

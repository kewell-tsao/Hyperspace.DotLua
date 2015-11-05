using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Irony.Interpreter
{

    // A class for special reserved None value used in many scripting languages. 
    public class NoneClass
    {
        string _toString;

        private NoneClass()
        {
            _toString = Resources.LabelNone;
        }
        public NoneClass(string toString)
        {
            _toString = toString;
        }
        public override string ToString()
        {
            return _toString;
        }

        public static NoneClass Value = new NoneClass();
    }



}

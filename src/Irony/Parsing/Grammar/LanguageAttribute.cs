using System;
#if DNXCORE50
using System.Linq;
using System.Reflection;
#endif

namespace Irony.Parsing
{
    [AttributeUsage(AttributeTargets.Class)]
    public class LanguageAttribute : Attribute
    {
        public LanguageAttribute() : this(null)
        {
        }

        public LanguageAttribute(string languageName) : this(languageName, "1.0", string.Empty)
        {
        }

        public LanguageAttribute(string languageName, string version, string description)
        {
            LanguageName = languageName;
            Version = version;
            Description = description;
        }

        public string LanguageName { get; }
        public string Version { get; }
        public string Description { get; }

        public static LanguageAttribute GetValue(Type grammarClass)
        {
#if DNXCORE50
            object[] attrs = grammarClass.GetTypeInfo().GetCustomAttributes(typeof(LanguageAttribute), true).Cast<object>().ToArray();
#else
            var attrs = grammarClass.GetCustomAttributes(typeof (LanguageAttribute), true);
#endif
            if (attrs != null && attrs.Length > 0)
            {
                var la = attrs[0] as LanguageAttribute;
                return la;
            }
            return null;
        }
    } //class
} //namespace
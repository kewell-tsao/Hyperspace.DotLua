using System.Collections.Generic;

namespace Irony.Parsing
{
    public enum WikiTermType
    {
        Text,
        Element,
        Format,
        Heading,
        List,
        Block,
        Table
    }

    public abstract class WikiTerminalBase : Terminal
    {
        public readonly string OpenTag, CloseTag;
        public readonly WikiTermType TermType;
        public string ContainerOpenHtmlTag, ContainerCloseHtmlTag;
        public string HtmlElementName, ContainerHtmlElementName;
        public string OpenHtmlTag, CloseHtmlTag;

        public WikiTerminalBase(string name, WikiTermType termType, string openTag, string closeTag,
            string htmlElementName) : base(name)
        {
            TermType = termType;
            OpenTag = openTag;
            CloseTag = closeTag;
            HtmlElementName = htmlElementName;
            Priority = TerminalPriority.Normal + OpenTag.Length; //longer tags have higher priority
        }

        public override IList<string> GetFirsts()
        {
            return new[] {OpenTag};
        }

        public override void Init(GrammarData grammarData)
        {
            base.Init(grammarData);
            if (!string.IsNullOrEmpty(HtmlElementName))
            {
                if (string.IsNullOrEmpty(OpenHtmlTag)) OpenHtmlTag = "<" + HtmlElementName + ">";
                if (string.IsNullOrEmpty(CloseHtmlTag)) CloseHtmlTag = "</" + HtmlElementName + ">";
            }
            if (!string.IsNullOrEmpty(ContainerHtmlElementName))
            {
                if (string.IsNullOrEmpty(ContainerOpenHtmlTag))
                    ContainerOpenHtmlTag = "<" + ContainerHtmlElementName + ">";
                if (string.IsNullOrEmpty(ContainerCloseHtmlTag))
                    ContainerCloseHtmlTag = "</" + ContainerHtmlElementName + ">";
            }
        }
    } //class
} //namespace
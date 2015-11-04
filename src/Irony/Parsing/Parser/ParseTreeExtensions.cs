using System.IO;
using System.Xml;

namespace Irony.Parsing
{
#if !SILVERLIGHT
    public static class ParseTreeExtensions
    {
        public static string ToXml(this ParseTree parseTree)
        {
            if (parseTree == null || parseTree.Root == null) return string.Empty;
            var xdoc = ToXmlDocument(parseTree);
            var sw = new StringWriter();
            var xw = XmlWriter.Create(sw);
            xw.Settings.Indent = true;
            xdoc.WriteTo(xw);
            xw.Flush();
            return sw.ToString();
        }

        public static XmlDocument ToXmlDocument(this ParseTree parseTree)
        {
            var xdoc = new XmlDocument();
            if (parseTree == null || parseTree.Root == null) return xdoc;
            var xTree = xdoc.CreateElement("ParseTree");
            xdoc.AppendChild(xTree);
            var xRoot = parseTree.Root.ToXmlElement(xdoc);
            xTree.AppendChild(xRoot);
            return xdoc;
        }

        public static XmlElement ToXmlElement(this ParseTreeNode node, XmlDocument ownerDocument)
        {
            var xElem = ownerDocument.CreateElement("Node");
            xElem.SetAttribute("Term", node.Term.Name);
            var term = node.Term;
            if (term.HasAstConfig() && term.AstConfig.NodeType != null)
                xElem.SetAttribute("AstNodeType", term.AstConfig.NodeType.Name);
            if (node.Token != null)
            {
                xElem.SetAttribute("Terminal", node.Term.GetType().Name);
                //xElem.SetAttribute("Text", node.Token.Text);
                if (node.Token.Value != null)
                    xElem.SetAttribute("Value", node.Token.Value.ToString());
            }
            else
                foreach (var child in node.ChildNodes)
                {
                    var xChild = child.ToXmlElement(ownerDocument);
                    xElem.AppendChild(xChild);
                }
            return xElem;
        } //method
    } //class
#endif
} //namespace
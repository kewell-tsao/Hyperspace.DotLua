using System;
using Irony.Parsing;

namespace Irony.Ast
{
    public class AstNodeEventArgs : EventArgs
    {
        public readonly ParseTreeNode ParseTreeNode;

        public AstNodeEventArgs(ParseTreeNode parseTreeNode)
        {
            ParseTreeNode = parseTreeNode;
        }

        public object AstNode
        {
            get { return ParseTreeNode.AstNode; }
        }
    }

    public delegate void AstNodeCreator(AstContext context, ParseTreeNode parseNode);

    public delegate object DefaultAstNodeCreator();

    public class AstNodeConfig
    {
        public object Data; //config data passed to AstNode

        public DefaultAstNodeCreator DefaultNodeCreator;
            //default method for creating AST nodes; compiled dynamic method, wrapper around "new nodeType();"

        public AstNodeCreator NodeCreator; // a custom method for creating AST nodes
        public Type NodeType;
        // An optional map (selector, filter) of child AST nodes. This facility provides a way to adjust the "map" of child nodes in various languages to 
        // the structure of a standard AST nodes (that can be shared betweeen languages). 
        // ParseTreeNode object has two properties containing list nodes: ChildNodes and MappedChildNodes.
        //  If term.AstPartsMap is null, these two child node lists are identical and contain all child nodes. 
        // If AstParts is not null, then MappedChildNodes will contain child nodes identified by indexes in the map. 
        // For example, if we set  
        //           term.AstPartsMap = new int[] {1, 4, 2}; 
        // then MappedChildNodes will contain 3 child nodes, which are under indexes 1, 4, 2 in ChildNodes list.
        // The mapping is performed in CoreParser.cs, method CheckCreateMappedChildNodeList.
        public int[] PartsMap;

        public bool CanCreateNode()
        {
            return NodeCreator != null || NodeType != null;
        }
    } //AstNodeConfig class
}
using System;
using System.Reflection.Emit;
using Irony.Parsing;
#if DNXCORE50
using System.Reflection;
#endif

namespace Irony.Ast
{
    public class AstBuilder
    {
        public AstContext Context;

        public AstBuilder(AstContext context)
        {
            Context = context;
        }

        public virtual void BuildAst(ParseTree parseTree)
        {
            if (parseTree.Root == null)
                return;
            Context.Messages = parseTree.ParserMessages;
            if (!Context.Language.AstDataVerified)
                VerifyLanguageData();
            if (Context.Language.ErrorLevel == GrammarErrorLevel.Error)
                return;
            BuildAst(parseTree.Root);
        }

        public virtual void VerifyLanguageData()
        {
            var gd = Context.Language.GrammarData;
            //Collect all terminals and non-terminals
            var terms = new BnfTermSet();
            //SL does not understand co/contravariance, so doing merge one-by-one
            foreach (var t in gd.Terminals) terms.Add(t);
            foreach (var t in gd.NonTerminals) terms.Add(t);
            var missingList = new BnfTermList();
            foreach (var term in terms)
            {
                var terminal = term as Terminal;
                if (terminal != null && terminal.Category != TokenCategory.Content) continue; //only content terminals
                if (term.Flags.IsSet(TermFlags.NoAstNode)) continue;
                var config = term.AstConfig;
                if (config.NodeCreator != null || config.DefaultNodeCreator != null) continue;
                //We must check NodeType
                if (config.NodeType == null)
                    config.NodeType = GetDefaultNodeType(term);
                if (config.NodeType == null)
                    missingList.Add(term);
                else
                    config.DefaultNodeCreator = CompileDefaultNodeCreator(config.NodeType);
            }
            if (missingList.Count > 0)
                // AST node type is not specified for term {0}. Either assign Term.AstConfig.NodeType, or specify default type(s) in AstBuilder.  
                Context.AddMessage(ErrorLevel.Error, SourceLocation.Empty, Resources.ErrNodeTypeNotSetOn,
                    string.Join(", ", missingList));
            Context.Language.AstDataVerified = true;
        }

        protected virtual Type GetDefaultNodeType(BnfTerm term)
        {
            if (term is NumberLiteral || term is StringLiteral)
                return Context.DefaultLiteralNodeType;
            if (term is IdentifierTerminal)
                return Context.DefaultIdentifierNodeType;
            return Context.DefaultNodeType;
        }

        public virtual void BuildAst(ParseTreeNode parseNode)
        {
            var term = parseNode.Term;
            if (term.Flags.IsSet(TermFlags.NoAstNode) || parseNode.AstNode != null) return;
            //children first
            var processChildren = !parseNode.Term.Flags.IsSet(TermFlags.AstDelayChildren) &&
                                  parseNode.ChildNodes.Count > 0;
            if (processChildren)
            {
                var mappedChildNodes = parseNode.GetMappedChildNodes();
                for (var i = 0; i < mappedChildNodes.Count; i++)
                    BuildAst(mappedChildNodes[i]);
            }
            //create the node
            //We know that either NodeCreator or DefaultNodeCreator is set; VerifyAstData create the DefaultNodeCreator
            var config = term.AstConfig;
            if (config.NodeCreator != null)
            {
                config.NodeCreator(Context, parseNode);
                // We assume that Node creator method creates node and initializes it, so parser does not need to call 
                // IAstNodeInit.Init() method on node object. But we do call AstNodeCreated custom event on term.
            }
            else
            {
                //Invoke the default creator compiled when we verified the data
                parseNode.AstNode = config.DefaultNodeCreator();
                //Initialize node
                var iInit = parseNode.AstNode as IAstNodeInit;
                if (iInit != null)
                    iInit.Init(Context, parseNode);
            }
            //Invoke the event on term
            term.OnAstNodeCreated(parseNode);
        } //method
        //Contributed by William Horner (wmh)
        private DefaultAstNodeCreator CompileDefaultNodeCreator(Type nodeType)
        {
            var constr = nodeType.GetConstructor(Type.EmptyTypes);
            var method = new DynamicMethod("CreateAstNode", nodeType, Type.EmptyTypes);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Newobj, constr);
            il.Emit(OpCodes.Ret);
            var result = (DefaultAstNodeCreator) method.CreateDelegate(typeof (DefaultAstNodeCreator));
            return result;
        }
    } //class
}
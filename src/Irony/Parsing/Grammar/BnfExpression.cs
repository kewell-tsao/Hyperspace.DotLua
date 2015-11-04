using System;
using System.Collections.Generic;

namespace Irony.Parsing
{
    //BNF expressions are represented as OR-list of Plus-lists of BNF terms
    internal class BnfExpressionData : List<BnfTermList>
    {
        public override string ToString()
        {
            try
            {
                var pipeArr = new string[Count];
                for (var i = 0; i < Count; i++)
                {
                    var seq = this[i];
                    var seqArr = new string[seq.Count];
                    for (var j = 0; j < seq.Count; j++)
                        seqArr[j] = seq[j].ToString();
                    pipeArr[i] = string.Join("+", seqArr);
                }
                return string.Join("|", pipeArr);
            }
            catch (Exception e)
            {
                return "(error: " + e.Message + ")";
            }
        }
    }

    public class BnfExpression : BnfTerm
    {
        internal BnfExpressionData Data;

        public BnfExpression(BnfTerm element) : this()
        {
            Data[0].Add(element);
        }

        public BnfExpression() : base(null)
        {
            Data = new BnfExpressionData();
            Data.Add(new BnfTermList());
        }

        public override string ToString()
        {
            return Data.ToString();
        }

        #region Implicit cast operators

        public static implicit operator BnfExpression(string symbol)
        {
            return new BnfExpression(Grammar.CurrentGrammar.ToTerm(symbol));
        }

        //It seems better to define one method instead of the following two, with parameter of type BnfTerm -
        // but that's not possible - it would be a conversion from base type of BnfExpression itself, which
        // is not allowed in c#
        public static implicit operator BnfExpression(Terminal term)
        {
            return new BnfExpression(term);
        }

        public static implicit operator BnfExpression(NonTerminal nonTerminal)
        {
            return new BnfExpression(nonTerminal);
        }

        #endregion
    } //class
} //namespace
namespace Irony.Parsing
{
    public class PrecedenceBasedParserAction : ConditionalParserAction
    {
        private readonly ReduceParserAction _reduceAction;
        private readonly ShiftParserAction _shiftAction;

        public PrecedenceBasedParserAction(BnfTerm shiftTerm, ParserState newShiftState, Production reduceProduction)
        {
            _reduceAction = new ReduceParserAction(reduceProduction);
            var reduceEntry = new ConditionalEntry(CheckMustReduce, _reduceAction, "(Precedence comparison)");
            ConditionalEntries.Add(reduceEntry);
            DefaultAction = _shiftAction = new ShiftParserAction(shiftTerm, newShiftState);
        }

        private bool CheckMustReduce(ParsingContext context)
        {
            var input = context.CurrentParserInput;
            var stackCount = context.ParserStack.Count;
            var prodLength = _reduceAction.Production.RValues.Count;
            for (var i = 1; i <= prodLength; i++)
            {
                var prevNode = context.ParserStack[stackCount - i];
                if (prevNode == null) continue;
                if (prevNode.Precedence == BnfTerm.NoPrecedence) continue;
                //if previous operator has the same precedence then use associativity
                if (prevNode.Precedence == input.Precedence)
                    return (input.Associativity == Associativity.Left); //if true then Reduce
                return (prevNode.Precedence > input.Precedence); //if true then Reduce
            }
            //If no operators found on the stack, do shift
            return false;
        }

        public override string ToString()
        {
            return string.Format(Resources.LabelActionOp, _shiftAction.NewState.Name,
                _reduceAction.Production.ToStringQuoted());
        }
    } //class
} //namespace
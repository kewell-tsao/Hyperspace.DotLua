using System.Collections.Generic;

namespace Irony.Parsing
{
    public enum PreferredActionType
    {
        Shift,
        Reduce
    }

    public class ConditionalParserAction : ParserAction
    {
        public ConditionalEntryList ConditionalEntries = new ConditionalEntryList();
        public ParserAction DefaultAction;

        public override void Execute(ParsingContext context)
        {
            var traceEnabled = context.TracingEnabled;
            if (traceEnabled) context.AddTrace("Conditional Parser Action.");
            for (var i = 0; i < ConditionalEntries.Count; i++)
            {
                var ce = ConditionalEntries[i];
                if (traceEnabled) context.AddTrace("  Checking condition: " + ce.Description);
                if (ce.Condition(context))
                {
                    if (traceEnabled) context.AddTrace("  Condition is TRUE, executing action: " + ce.Action);
                    ce.Action.Execute(context);
                    return;
                }
            }
            //if no conditions matched, execute default action
            if (DefaultAction == null)
            {
                context.AddParserError(
                    "Fatal parser error: no conditions matched in conditional parser action, and default action is null." +
                    " State: {0}", context.CurrentParserState.Name);
                context.Parser.RecoverFromError();
                return;
            }
            if (traceEnabled) context.AddTrace("  All conditions failed, executing default action: " + DefaultAction);
            DefaultAction.Execute(context);
        } //method

        #region embedded types

        public delegate bool ConditionChecker(ParsingContext context);

        public class ConditionalEntry
        {
            public ParserAction Action;
            public ConditionChecker Condition;
            public string Description; //for tracing

            public ConditionalEntry(ConditionChecker condition, ParserAction action, string description)
            {
                Condition = condition;
                Action = action;
                Description = description;
            }

            public override string ToString()
            {
                return Description + "; action: " + Action;
            }
        }

        public class ConditionalEntryList : List<ConditionalEntry>
        {
        }

        #endregion
    } //class
}
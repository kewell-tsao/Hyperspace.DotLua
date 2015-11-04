using System;
using System.Linq;
using System.Text;

namespace Irony.Parsing
{
    public static class ParserDataPrinter
    {
        public static string PrintStateList(LanguageData language)
        {
            var sb = new StringBuilder();
            foreach (var state in language.ParserData.States)
            {
                sb.Append("State " + state.Name);
                if (state.BuilderData.IsInadequate) sb.Append(" (Inadequate)");
                sb.AppendLine();
                var srConflicts = state.BuilderData.GetShiftReduceConflicts();
                if (srConflicts.Count > 0)
                    sb.AppendLine("  Shift-reduce conflicts on inputs: " + srConflicts);
                var ssConflicts = state.BuilderData.GetReduceReduceConflicts();
                if (ssConflicts.Count > 0)
                    sb.AppendLine("  Reduce-reduce conflicts on inputs: " + ssConflicts);
                //LRItems
                if (state.BuilderData.ShiftItems.Count > 0)
                {
                    sb.AppendLine("  Shift items:");
                    foreach (var item in state.BuilderData.ShiftItems)
                        sb.AppendLine("    " + item);
                }
                if (state.BuilderData.ReduceItems.Count > 0)
                {
                    sb.AppendLine("  Reduce items:");
                    foreach (var item in state.BuilderData.ReduceItems)
                    {
                        var sItem = item.ToString();
                        if (item.Lookaheads.Count > 0)
                            sItem += " [" + item.Lookaheads + "]";
                        sb.AppendLine("    " + sItem);
                    }
                }
                sb.Append("  Transitions: ");
                var atFirst = true;
                foreach (var key in state.Actions.Keys)
                {
                    var action = state.Actions[key] as ShiftParserAction;
                    if (action == null)
                        continue;
                    if (!atFirst) sb.Append(", ");
                    atFirst = false;
                    sb.Append(key);
                    sb.Append("->");
                    sb.Append(action.NewState.Name);
                }
                sb.AppendLine();
                sb.AppendLine();
            } //foreach
            return sb.ToString();
        }

        public static string PrintTerminals(LanguageData language)
        {
            var termList = language.GrammarData.Terminals.ToList();
            termList.Sort((x, y) => string.Compare(x.Name, y.Name));
            var result = string.Join(Environment.NewLine, termList);
            return result;
        }

        public static string PrintNonTerminals(LanguageData language)
        {
            var sb = new StringBuilder();
            var ntList = language.GrammarData.NonTerminals.ToList();
            ntList.Sort((x, y) => string.Compare(x.Name, y.Name));
            foreach (var nt in ntList)
            {
                sb.Append(nt.Name);
                sb.Append(nt.Flags.IsSet(TermFlags.IsNullable) ? "  (Nullable) " : string.Empty);
                sb.AppendLine();
                foreach (var pr in nt.Productions)
                {
                    sb.Append("   ");
                    sb.AppendLine(pr.ToString());
                }
            } //foreachc nt
            return sb.ToString();
        }
    } //class
} //namespace
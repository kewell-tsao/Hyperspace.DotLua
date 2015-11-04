namespace Irony.Parsing
{
    //GrammarData is a container for all basic info about the grammar
    // GrammarData is a field in LanguageData object. 
    public class GrammarData
    {
        public readonly BnfTermSet AllTerms = new BnfTermSet();
        public readonly Grammar Grammar;
        public readonly LanguageData Language;
        public readonly NonTerminalSet NonTerminals = new NonTerminalSet();
        public readonly TerminalSet Terminals = new TerminalSet();
        public NonTerminal AugmentedRoot;
        public NonTerminalSet AugmentedSnippetRoots = new NonTerminalSet();
        public TerminalSet NoPrefixTerminals = new TerminalSet(); //Terminals that have no limited set of prefixes

        public GrammarData(LanguageData language)
        {
            Language = language;
            Grammar = language.Grammar;
        }
    } //class
} //namespace
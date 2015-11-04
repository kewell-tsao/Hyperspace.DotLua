using System.Collections.Generic;

namespace Irony.Parsing
{
    public class TerminalLookupTable : Dictionary<char, TerminalList>
    {
    }

    // ScannerData is a container for all detailed info needed by scanner to read input. 
    public class ScannerData
    {
        public readonly LanguageData Language;
        public readonly TerminalList MultilineTerminals = new TerminalList();
        //hash table for fast lookup of non-grammar terminals by input char
        public readonly TerminalLookupTable NonGrammarTerminalsLookup = new TerminalLookupTable();

        public readonly TerminalLookupTable TerminalsLookup = new TerminalLookupTable();
            //hash table for fast terminal lookup by input char

        public TerminalList NoPrefixTerminals = new TerminalList();
            //Terminals with no limited set of prefixes, copied from GrammarData 

        public ScannerData(LanguageData language)
        {
            Language = language;
        }
    } //class
} //namespace
using Irony.Parsing.Construction;

namespace Irony.Parsing
{
    public class LanguageData
    {
        public readonly GrammarErrorList Errors = new GrammarErrorList();
        public readonly Grammar Grammar;
        public readonly GrammarData GrammarData;
        public readonly ParserData ParserData;
        public readonly ScannerData ScannerData;
        public bool AstDataVerified;
        public long ConstructionTime;
        public GrammarErrorLevel ErrorLevel = GrammarErrorLevel.NoError;

        public LanguageData(Grammar grammar)
        {
            Grammar = grammar;
            GrammarData = new GrammarData(this);
            ParserData = new ParserData(this);
            ScannerData = new ScannerData(this);
            ConstructAll();
        }

        public void ConstructAll()
        {
            var builder = new LanguageDataBuilder(this);
            builder.Build();
        }

        public bool CanParse()
        {
            return ErrorLevel < GrammarErrorLevel.Error;
        }
    } //class
} //namespace
namespace Irony.Parsing
{
    // Should be implemented by Grammar class to be able to run samples in Grammar Explorer.
    public interface ICanRunSample
    {
        string RunSample(RunSampleArgs args);
    }

    public class RunSampleArgs
    {
        public LanguageData Language;
        public ParseTree ParsedSample;
        public string Sample;

        public RunSampleArgs(LanguageData language, string sample, ParseTree parsedSample)
        {
            Language = language;
            Sample = sample;
            ParsedSample = parsedSample;
        }
    }
}
using CommandLine;

namespace JlzQualiTool
{
    public class Options
    {
        public static Options Current { get; private set; } = new Options();

        [Option("file", HelpText = "Path to data file that should be loaded.")]
        public string? File { get; set; }

        public static void Start(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(options => { Current = options; })
                .WithNotParsed(errors => { Current = new Options(); });
        }
    }
}
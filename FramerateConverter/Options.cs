using CommandLine;

//command line parameters:
//====================
//-b, --batch					no
//-s, --source					--
//-t, --target					--
//-k, --key-frames-only			no
//-f, --frames-per-second		input fps * 2
//-o, --overwrite				ask
//-v, --version					--
//-h, --help					--

namespace FramerateConverter
{
    public class Options
    {
        [Option('b', "batch", Required = false, HelpText = "Enable batch behavior: Run once, then terminate automatically.")]
        public bool BatchMode { get; set; }

        [Option('s', "source", Required = false, HelpText = "The input video.")]
        public string SourceFile { get; set; }

        [Option('t', "target", Required = false, HelpText = "The output video.")]
        public string TargetFile { get; set; }

        [Option('k', "key-frames-only", Required = false, HelpText = "Enables Key-Frame-Only videos.")]
        public bool KeyFramesOnly { get; set; }

        [Option('f', "frames-per-second", Required = false, HelpText = "The target FPS.")]
        public int? FramesPerSecond { get; set; }

        [Option('o', "overwrite", Required = false, HelpText = "If output video exists, it will be overwritten.")]
        public bool Overwrite { get; set; }
    }
}
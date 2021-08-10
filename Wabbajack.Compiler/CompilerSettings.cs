using System;
using Wabbajack.DTOs;
using Wabbajack.Paths;

namespace Wabbajack.Compiler
{
    public class CompilerSettings
    {
        public AbsolutePath Source { get; set; }
        public AbsolutePath Downloads { get; set; }
        public Game Game { get; set; }
        public AbsolutePath OutputFile { get; set; }
        public AbsolutePath OutputFolder => OutputFile.Parent;
        public bool UseGamePaths { get; set; }
        public Game[] OtherGames { get; set; } = Array.Empty<Game>();
        
        public TimeSpan MaxVerificationTime { get; set; }
    }
}
using System;
using Wabbajack.DTOs;
using Wabbajack.Paths;

namespace Wabbajack.Compiler
{
    public class CompilerSettings
    {
        public bool ModlistIsNSFW;
        public AbsolutePath Source { get; set; }
        public AbsolutePath Downloads { get; set; }
        public Game Game { get; set; }
        public AbsolutePath OutputFile { get; set; }
        
        public AbsolutePath ModListImage { get; set; }
        public bool UseGamePaths { get; set; }
        public Game[] OtherGames { get; set; } = Array.Empty<Game>();
        
        public TimeSpan MaxVerificationTime { get; set; } = TimeSpan.FromMinutes(1);
        public string ModListName { get; set; }
        public string ModListAuthor { get; set; }
        public string ModListDescription { get; set; }
        public string ModlistReadme { get; set; }
        public Uri? ModListWebsite { get; set; }
        public Version ModlistVersion { get; set; }
        public string[] SelectedProfiles { get; set; }
    }
}
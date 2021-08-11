using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Wabbajack.Paths;

namespace Wabbajack.Compiler
{
    public class Consts
    {
        public static RelativePath BSACreationDir = "TEMP_BSA_FILES".ToRelativePath();
        public static RelativePath MO2ModFolderName = "mods".ToRelativePath();
        public static RelativePath MO2Profiles = "profiles".ToRelativePath();
        public static RelativePath MO2Saves = "profiles".ToRelativePath();
        public static Version? CurrentMinimumWabbajackVersion = new("2.2.2.0");
        
        public static RelativePath GameFolderFilesDir = "Game Folder Files".ToRelativePath();
        public static RelativePath ManualGameFilesDir = "Manual Game Files".ToRelativePath();
        public static RelativePath LOOTFolderFilesDir = "LOOT Config Files".ToRelativePath();
        public static RelativePath MetaIni = "meta.ini".ToRelativePath();
        public static RelativePath ModListTxt = "modlist.txt".ToRelativePath();

        public static string WABBAJACK_INCLUDE = "WABBAJACK_INCLUDE";
        public static string WABBAJACK_ALWAYS_ENABLE = "WABBAJACK_ALWAYS_ENABLE";
        public static string WABBAJACK_ALWAYS_DISABLE = "WABBAJACK_ALWAYS_DISABLE";
        public static string WABBAJACK_NOMATCH_INCLUDE = "WABBAJACK_NOMATCH_INCLUDE";
        public static string WABBAJACK_IGNORE = "WABBAJACK_IGNORE";
        public static string WABBAJACK_NOMATCH_INCLUDE_FILES = "WABBAJACK_NOMATCH_INCLUDE_FILES.txt";
        public static string WABBAJACK_IGNORE_FILES = "WABBAJACK_IGNORE_FILES.txt";
        public static string WABBAJACK_INCLUDE_SAVES = "WABBAJACK_INCLUDE_SAVES";

        public static string LineSeparator => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\r\n" : "\n";
        
        public static readonly HashSet<Extension> SupportedBSAs = new[] {".bsa", ".ba2"}
            .Select(s => new Extension(s)).ToHashSet();
        
    }
}
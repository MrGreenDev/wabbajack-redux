using System;
using Wabbajack.Paths;

namespace Wabbajack.Compiler
{
    public class Consts
    {
        public static RelativePath BSACreationDir = "TEMP_BSA_FILES".ToRelativePath();
        public static RelativePath MO2ModFolderName = "mods".ToRelativePath();
        public static RelativePath MO2ProfilesFolderName = "profiles".ToRelativePath();
        public static Version? CurrentMinimumWabbajackVersion = new("2.2.2.0");
        
        public static RelativePath GameFolderFilesDir = "Game Folder Files".ToRelativePath();
        public static RelativePath ManualGameFilesDir = "Manual Game Files".ToRelativePath();
        public static RelativePath LOOTFolderFilesDir = "LOOT Config Files".ToRelativePath();
        public static RelativePath MetaIni = "meta.ini".ToRelativePath();
    }
}
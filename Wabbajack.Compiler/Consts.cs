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
    }
}
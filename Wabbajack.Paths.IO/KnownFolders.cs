using System;

namespace Wabbajack.Paths.IO
{
    public static class KnownFolders
    {
        public static AbsolutePath EntryPoint => Environment.ProcessPath!.ToAbsolutePath().Parent;

        public static AbsolutePath AppDataLocal =>
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).ToAbsolutePath();

        public static AbsolutePath WabbajackAppLocal => AppDataLocal.Combine("Wabbajack");

    }
}
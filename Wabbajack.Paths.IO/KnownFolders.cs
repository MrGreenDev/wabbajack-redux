namespace Wabbajack.Paths.IO
{
    public static class KnownFolders
    {
        public static AbsolutePath EntryPoint => ((AbsolutePath)typeof(KnownFolders).Assembly.Location).Parent;
    }
}
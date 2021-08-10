using System.Collections.Generic;
using System.Linq;
using Wabbajack.VFS;

namespace Wabbajack.Compiler.CompilationSteps
{
    public class IncludePatches
    {
        public static (PatchCache.CacheEntry, VirtualFile) PickPatch(ACompiler compiler, IEnumerable<(PatchCache.CacheEntry? data, VirtualFile file)> patches)
        {
            var ordered = patches
                .Select(f => (f.data!, f.file))
                .OrderBy(f => f.Item1.PatchSize)
                .ToArray();

            var primaryChoice = ordered.FirstOrDefault(itm =>
            {
                var baseHash = itm.file.TopParent.Hash;
                
                // If this file doesn't come from a game use it
                if (!compiler.GamesWithHashes.TryGetValue(baseHash, out var games))
                    return true;

                // Otherwise skip files that are not from the primary game
                return games.Contains(compiler._settings.Game);
            });
            
            // If we didn't find a file from an archive or the primary game, use a secondary game file.
            var result = primaryChoice != default ? primaryChoice : ordered.FirstOrDefault();
            return result;
        }
    }
}
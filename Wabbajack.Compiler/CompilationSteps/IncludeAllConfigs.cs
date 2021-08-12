﻿using System.Threading.Tasks;
using Wabbajack.DTOs;
using Wabbajack.DTOs.Directives;
using Wabbajack.Paths.IO;

namespace Wabbajack.Compiler.CompilationSteps
{
    public class IncludeAllConfigs : ACompilationStep
    {
        public IncludeAllConfigs(ACompiler compiler) : base(compiler)
        {
        }

        public override async ValueTask<Directive?> Run(RawSourceFile source)
        {
            if (!Consts.ConfigFileExtensions.Contains(source.Path.Extension)) return null;
            var result = source.EvolveTo<InlineFile>();
            result.SourceDataID = await _compiler.IncludeFile(await source.AbsolutePath.ReadAllBytesAsync());
            return result;
        }
    }
}

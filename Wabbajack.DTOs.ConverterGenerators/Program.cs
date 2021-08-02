using System;
using System.IO;
using System.Net;
using Wabbajack.DTOs.BSA.ArchiveStates;
using Wabbajack.DTOs.BSA.FileStates;
using Wabbajack.DTOs.DownloadStates;
using Wabbajack.DTOs.JsonConverters;

namespace Wabbajack.DTOs.ConverterGenerators
{
    class Program
    {
        static void Main(string[] args)
        {
            var cfile = new CFile();
            new PolymorphicGenerator<IDownloadState>().GenerateAll(cfile);
            new PolymorphicGenerator<IArchive>().GenerateAll(cfile);
            new PolymorphicGenerator<Directive>().GenerateAll(cfile);
            new PolymorphicGenerator<AFile>().GenerateAll(cfile);

            
            
            
            
            cfile.Write(@"..\Wabbajack.DTOs\JsonConverters\Generated.cs");
        }
        
    }
}
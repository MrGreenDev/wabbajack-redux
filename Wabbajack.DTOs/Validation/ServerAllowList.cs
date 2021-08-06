using System;
using System.Collections.Generic;

namespace Wabbajack.DTOs.Validation
{
    public class ServerAllowList
    {
        public string[] GoogleIDs = Array.Empty<string>();
        public string[] AllowedPrefixes = Array.Empty<string>();
    }
}
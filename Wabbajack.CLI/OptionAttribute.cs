using System;

namespace Wabbajack.CLI
{
    public class OptionAttribute : Attribute
    {
        public string LongName { get; set; }
        public char ShortName { get; set; }
        public bool Required { get; set; } = true;
        public string HelpText { get; set; } = "";
        public OptionAttribute(char shortName, string longName)
        {
            ShortName = shortName;
            LongName = longName;
        }

    }
}
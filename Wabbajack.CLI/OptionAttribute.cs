using System;

namespace Wabbajack.CLI
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class OptionAttribute : Attribute
    {
        public char ShortName { get; set; }
        public bool Required { get; set; } = true;
        public string HelpText { get; set; } = "";
        public OptionAttribute(char shortName)
        {
            ShortName = shortName;
        }

    }
}
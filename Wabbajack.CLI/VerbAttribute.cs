using System;

namespace Wabbajack.CLI
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class VerbAttribute : Attribute
    {
        public VerbAttribute(string shortName)
        {
            ShortName = shortName;
        }

        public string ShortName { get; set; }
        public string HelpText { get; set; }
    }
}
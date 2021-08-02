using System;

namespace Wabbajack.DTOs.JsonConverters
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class JsonAliasAttribute : Attribute
    {
        public string Alias { get; }
        public JsonAliasAttribute(string alias)
        {
            Alias = alias;
        }
    }
}
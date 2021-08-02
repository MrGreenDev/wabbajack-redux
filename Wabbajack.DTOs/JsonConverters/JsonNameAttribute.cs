using System;

namespace Wabbajack.DTOs.JsonConverters
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class JsonNameAttribute : Attribute
    {
        public string Name { get; }
        public JsonNameAttribute(string name)
        {
            Name = name;
        }
    }
}
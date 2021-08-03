using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Wabbajack.CLI.Verbs
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    public abstract class AVerb<T> : IVerb
    where T : AVerb<T>
    {
        protected  ILogger<T> Logger { get; }
        public AVerb(ILogger<T> logger)
        {
            Logger = logger;
        }

        public Command MakeCommand()
        {
            var attr = (VerbAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(VerbAttribute))!;
            var command = new Command(attr.ShortName, attr.HelpText);
            var method = typeof(T).GetMethod("Run");
            if (method == null)
                throw new NullReferenceException("All Verbs must have a public Run method");
            command.Handler = CommandHandler.Create(method, this);

            foreach (var prop in Attribute.GetCustomAttributes(method!, typeof(OptionAttribute)))
            {
                var propAttr = (OptionAttribute)prop;
                var type = method.GetParameters().First(p => p.Name == propAttr.LongName).ParameterType;
                var option = new Option(propAttr.LongName, propAttr.HelpText, type)
                {
                    IsRequired = propAttr.Required
                };
                
                
                option.AddAlias(propAttr.ShortName.ToString());

                
                command.AddOption(option);
            }
            return command;
        }
    }
}
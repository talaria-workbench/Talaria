using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace Talaria.AddIn.Generator
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        private const string attributeText = @"

";



        public void Initialize(GeneratorInitializationContext context)
        {
            // Register the attribute source
            context.RegisterForPostInitialization((i) => i.AddSource("TalariaAttributes.cs", attributeText));

        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                ExecuteInternal(context);

            }
            catch (Exception e)
            {

                string lines = string.Empty;
                var reader = new StringReader(e.ToString());

                string line;
                do
                {
                    line = reader.ReadLine();
                    lines += "\n#error " + line ?? string.Empty;
                }
                while (line != null);

                var txt = SourceText.From(lines, System.Text.Encoding.UTF8);

                context.AddSource($"Errors.cs", txt);
            }
        }

        private static void ExecuteInternal(GeneratorExecutionContext context)
        {

        }
    }

}
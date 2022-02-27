
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.Text;

namespace Talaria.AddIn.Generator;

[Generator]
public class Generator : ISourceGenerator
{
    private const string attributeText = @"

";



    public void Initialize(GeneratorInitializationContext context)
    {
        //if (!System.Diagnostics.Debugger.IsAttached) {
        //    _ = System.Diagnostics.Debugger.Launch();
        //}
        // Register the attribute source
        context.RegisterForPostInitialization((i) => i.AddSource("TalariaAttributes.cs", attributeText));
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());


    }

    public void Execute(GeneratorExecutionContext context)
    {
        try {
            ExecuteInternal(context);

        } catch (Exception e) {

            var lines = string.Empty;
            var reader = new StringReader(e.ToString());

            string line;
            do {
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
        if (context.SyntaxContextReceiver is not SyntaxReceiver receiver) {
            return;
        }

        foreach (var c in receiver.Classes) {
            // We already checked that basetype is not null in SyntaxReciver
            var source = SyntaxReceiver.GetUnboundName(c.BaseType!) switch
            {
                SyntaxReceiver.CREATE_ITEM_TYPE => ProcessClass(c, _ => "[System.ComponentModel.Composition.Export]"),
                SyntaxReceiver.COMPONENT_BASE_TYPE => ProcessClass(c, _ => $@"[System.ComponentModel.Composition.Export]"),
                SyntaxReceiver.LOADER_BASE_TYPE => ProcessClass(c, _ => $@"
[System.ComponentModel.Composition.Export]"),
                _ => null
            };
            if (source is not null) {

                var b = new StringBuilder();
                ISymbol symbol = c;
                while (symbol is not null) {
                    if (b.Length != 0) {
                        _ = b.Append('.');
                    }

                    _ = b.Append(symbol.MetadataName);
                    symbol = symbol.ContainingSymbol;
                }
                context.AddSource($"{b}_addin_generator.cs", SourceText.From(source, Encoding.UTF8));
            }

        }
    }

    private static string? ProcessClass(INamedTypeSymbol classSymbol, Func<INamedTypeSymbol, string>? generateAttributes = null, Func<INamedTypeSymbol, string>? generateBody = null)
    {


        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var source = new StringBuilder();
        // begin building the generated source

        _ = source.Append($@"
#nullable enable
namespace {namespaceName}
{{
");

        var queue = new Queue<ITypeSymbol>();
        var containing = classSymbol.ContainingSymbol;
        while (!containing.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default)) {
            if (containing is ITypeSymbol type) {
                queue.Enqueue(type);
            } else {
                //TODO: issue a diagnostic that the containing type is not supported
                return null;
            }
            containing = containing.ContainingSymbol;

        }

        var additionalClasses = queue.Count;

        while (queue.Count > 0) {
            var type = queue.Dequeue();
            _ = source.Append($@"
    partial {GetKind(type)} {type.Name}
    {{
");
        }
        _ = source.Append($@"
    {generateAttributes?.Invoke(classSymbol) ?? ""}
    partial {GetKind(classSymbol)} {classSymbol.Name}
    {{
        {generateBody?.Invoke(classSymbol) ?? ""}
");




        for (var i = 0; i < additionalClasses; i++) {
            _ = source.Append("} ");
        }
        _ = source.Append("} }");
        return source.ToString();
    }
    private static string GetKind(ITypeSymbol classSymbol)
    {
        var v = classSymbol.TypeKind switch
        {
            TypeKind.Class => "class",
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            _ => throw new NotImplementedException($"GetKind is currently does not support {classSymbol}")
        };
        if (classSymbol.IsRecord) {
            v = $"record {v}";
        }

        return v;
    }
}


/// <summary>
/// Created on demand before each generation pass
/// </summary>
internal class SyntaxReceiver : ISyntaxContextReceiver
{
    public List<INamedTypeSymbol> Classes { get; } = new();

    public const string CREATE_ITEM_TYPE = "Talaria.AddIn.CreateItemBase";
    public const string COMPONENT_BASE_TYPE = "Talaria.AddIn.ComponentBase<,>";
    public const string LOADER_BASE_TYPE = "Talaria.AddIn.ComponentLoaderBase<,>";
    private static readonly string[] metadataNames = new[]{
        CREATE_ITEM_TYPE,
        COMPONENT_BASE_TYPE,
        LOADER_BASE_TYPE
    };

    /// <summary>
    /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
    /// </summary>
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        // any field with at least one attribute is a candidate for property generation
        if (context.Node is ClassDeclarationSyntax classDeclarationSyntax) {
            if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is INamedTypeSymbol classSymbol && classSymbol.BaseType is not null && metadataNames.Contains(GetUnboundName(classSymbol.BaseType))) {
                this.Classes.Add(classSymbol);
            }
        }
    }

    public static string? GetUnboundName(INamedTypeSymbol classSymbol) =>
        classSymbol.IsGenericType
        ? classSymbol.ConstructUnboundGenericType().ToDisplayString()
        : classSymbol.ToDisplayString();
}
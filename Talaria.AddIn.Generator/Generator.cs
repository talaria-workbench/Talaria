
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
            var source = SyntaxReceiver.IsDecendantOfAny(c) switch
            {
                SyntaxReceiver.CREATE_ITEM_TYPE => ProcessClass(c, GenerateAttributesIncludingBaseClass),
                SyntaxReceiver.COMPONENT_BASE_TYPE => ProcessClass(c, GenerateAttributesIncludingBaseClass),
                SyntaxReceiver.LOADER_BASE_TYPE => ProcessClass(c, GenerateAttributesIncludingBaseClass),
                SyntaxReceiver.CREATE_ITEM_OPTIONS_BASE_TYPE => ProcessClass(c, generateBody: c => GenerateOptionsBody(c, context)),
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

    private static string GenerateOptionsBody(INamedTypeSymbol arg, GeneratorExecutionContext context)
    {


        var implementaiton = arg.GetMembers("DefaultOptions").OfType<IMethodSymbol>().FirstOrDefault(x => (x.OverriddenMethod?.ContainingSymbol.ToDisplayString() ?? null) == SyntaxReceiver.CREATE_ITEM_OPTIONS_BASE_TYPE);
        if (implementaiton is null) {
            return string.Empty;
        }
        var str = new StringBuilder();
        var expression = implementaiton.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).OfType<MethodDeclarationSyntax>().FirstOrDefault(x => x.ExpressionBody is not null)?.ExpressionBody;
        //var expression = implementaiton.DeclaringSyntaxReferences.OfType<ArrowExpressionClauseSyntax>().FirstOrDefault();
        if (expression is null) {
            return string.Empty;
        }
        if (expression.Expression is not ArrayCreationExpressionSyntax arrayExpression) {
            return string.Empty;
        }
        if (arrayExpression.Initializer is null) {
            return string.Empty;
        }
        var semanticModel = context.Compilation.GetSemanticModel(arrayExpression.SyntaxTree);

        ;

        var body = new StringBuilder();
        var list = new List<(int index, string name, ITypeSymbol currentType, ITypeSymbol? valueType)>();
        {
            var index = -1;
            foreach (var e in arrayExpression.Initializer.Expressions) {
                index++;
                if (e is not ObjectCreationExpressionSyntax createionExpreiont) {
                    continue;
                }

                var firstArgument = createionExpreiont.ArgumentList?.Arguments.FirstOrDefault();
                if (firstArgument is null || firstArgument.Expression is not LiteralExpressionSyntax literalExpression || literalExpression.Kind() != SyntaxKind.StringLiteralExpression) {
                    continue;
                }

                var name = Normalize(literalExpression.Token.ValueText);
                var type = semanticModel.GetTypeInfo(createionExpreiont).Type;
                var valueType = GetValuetype(type);
                ITypeSymbol? GetValuetype(ITypeSymbol? startType)
                {
                    if (startType is null) {
                        return null;
                    }
                    if (startType is INamedTypeSymbol symbol && symbol.IsGenericType) {
                        var unboundSymbol = symbol.ConstructUnboundGenericType();
                        if (unboundSymbol.ToDisplayString() == "Talaria.AddIn.BaseOption<>") {
                            return symbol.TypeArguments.First();
                        }
                    }
                    return GetValuetype(startType.BaseType);
                }

                static string Normalize(string str)
                {
                    var value = new StringBuilder(str);
                    value[0] = char.ToLowerInvariant(value[0]);
                    for (var i = value.Length - 1; i >= 0; i--) {
                        if (value[i] == ' ') {
                            if (i + 1 < value.Length) {
                                value[i + 1] = char.ToUpperInvariant(value[i + 1]);
                            }
                            _ = value.Remove(i, 1);
                        }
                    }
                    return value.ToString();
                }
                if (type is null) {
                    continue;
                }

                list.Add((index, name, type, valueType));
                _ = body.AppendLine($@"
    private {type.ToDisplayString()} {name}Option => (({type.ToDisplayString()}) this.Options[{index}]);
");
            }
        }

        _ = body.AppendLine($@"    public new Configuration GetConfiguration()
    {{
        return new Configuration(base.GetConfiguration());
    }}");
        _ = body.AppendLine($@"public class Configuration {{");

        foreach (var (index, name, type, valueType) in list) {
            if (valueType is not null) {
                _ = body.AppendLine($@"
    public {valueType.ToDisplayString()} {name}{{get;}}
");
            }
        }

        _ = body.AppendLine($@"public Configuration(Talaria.AddIn.Configuration[] configurations) {{");

        foreach (var (index, name, type, valueType) in list) {
            if (valueType is not null) {
                _ = body.AppendLine($@"
    this.{name} = ((Talaria.AddIn.Configuration<{valueType.ToDisplayString()}>) configurations[{index}]).Value;
");
            }
        }
        _ = body.AppendLine("}");
        _ = body.AppendLine("}");


        return body.ToString();

    }

    private static string GenerateAttributesIncludingBaseClass(INamedTypeSymbol? c)
    {
        var attributes = new StringBuilder();
        while (c is not null && c.ToDisplayString() != "object") {
            //if (c.IsGenericType) {
            //    _ = attributes.AppendLine($"[System.ComponentModel.Composition.Export(typeof({c.ConstructUnboundGenericType().ToDisplayString()}))]");
            //}
            _ = attributes.AppendLine($"[System.ComponentModel.Composition.Export(typeof({c.ToDisplayString()}))]");
            c = c.BaseType;
        }
        return attributes.ToString();
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
    public const string CREATE_ITEM_OPTIONS_BASE_TYPE = "Talaria.AddIn.CreateItemOptions";
    private static readonly string[] metadataNames = new[]{
        CREATE_ITEM_TYPE,
        COMPONENT_BASE_TYPE,
        LOADER_BASE_TYPE,
        CREATE_ITEM_OPTIONS_BASE_TYPE,
    };

    /// <summary>
    /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
    /// </summary>
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        // any field with at least one attribute is a candidate for property generation
        if (context.Node is ClassDeclarationSyntax classDeclarationSyntax) {
            if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is INamedTypeSymbol classSymbol && !classSymbol.IsAbstract && classSymbol.BaseType is not null && IsDecendantOfAny(classSymbol) is not null) {
                this.Classes.Add(classSymbol);
            }
        }
    }

    public static string? IsDecendantOfAny(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType is not null) {
            var unboundName = GetUnboundName(baseType);
            if (metadataNames.Contains(unboundName)) {
                return unboundName;
            }
            baseType = baseType.BaseType;
        }
        return null;
    }

    public static string? GetUnboundName(INamedTypeSymbol classSymbol) =>
        classSymbol.IsGenericType
        ? classSymbol.ConstructUnboundGenericType().ToDisplayString()
        : classSymbol.ToDisplayString();
}
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerators;

public readonly record struct GenerationVerification(string ClassName, string NodeName)
{
    public readonly string ClassName = ClassName;
    public readonly string NodeName = NodeName;

    public static GenerationVerification? FromAttribute(
        SemanticModel semanticModel,
        INamedTypeSymbol classSymbol,
        AttributeSyntax attributeSyntax
    )
    {
        if (
            semanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol
        )
            return null;

        var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
        var fullName = attributeContainingTypeSymbol.ToDisplayString();

        // Is this the GenerateNodeGetter attribute?
        if (fullName != "NodeGetterGenerators.VerifyNodeGettersAttribute")
            return null;

        // Get the attribute constructor arguments
        if (attributeSyntax.ArgumentList?.Arguments.Count != 1)
            return null;

        var arguments = attributeSyntax.ArgumentList.Arguments;

        if (semanticModel.GetConstantValue(arguments[0].Expression).Value is not string nodeName)
            return null;

        return new GenerationVerification(classSymbol.ToDisplayString(), nodeName);
    }
}

public readonly record struct GetterToGenerate(
    string NodeType,
    string NodePath,
    bool IsCached,
    string ClassName
)
{
    public readonly string NodeType = NodeType;
    public readonly string NodePath = NodePath;
    public readonly bool IsCached = IsCached;
    public readonly string ClassName = ClassName;

    public string MethodName => "GetNode" + ConvertPathToMethodName(NodePath);

    private static string ConvertPathToMethodName(string path)
    {
        // Simple conversion: remove non-alphanumeric characters and capitalize first letter
        var sb = new StringBuilder();
        var capitalizeNext = true;

        foreach (var c in path)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(capitalizeNext ? char.ToUpper(c) : c);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }

        return sb.ToString();
    }

    public static GetterToGenerate? FromAttribute(
        SemanticModel semanticModel,
        INamedTypeSymbol classSymbol,
        AttributeSyntax attributeSyntax
    )
    {
        if (
            semanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol
        )
            return null;

        var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
        var fullName = attributeContainingTypeSymbol.ToDisplayString();

        // Is this the GenerateNodeGetter attribute?
        if (fullName != "NodeGetterGenerators.GenerateNodeGetterAttribute")
            return null;

        // Get the attribute constructor arguments
        if (attributeSyntax.ArgumentList == null)
            return null;
        var arguments = attributeSyntax.ArgumentList.Arguments;
        if (arguments.Count is < 2 or > 3)
            return null;

        // Handle typeof(X) expression for first argument
        if (arguments[0].Expression is not TypeOfExpressionSyntax typeOfExpression)
            return null;

        // Get the type info from the typeof expression
        var typeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type);
        if (typeInfo.Type == null)
            return null;

        var nodeType = typeInfo.Type.ToDisplayString();

        // Get the node path (second parameter)
        if (semanticModel.GetConstantValue(arguments[1].Expression).Value is not string nodePath)
            return null;

        // Get caching option (third parameter)
        var isCached = false;
        if (arguments.Count > 2)
        {
            var isCachedOpt = semanticModel.GetConstantValue(arguments[2].Expression).Value;
            if (isCachedOpt is not bool x)
                return null;
            isCached = x;
        }

        return new GetterToGenerate(nodeType, nodePath, isCached, classSymbol.ToDisplayString());
    }
}

public static class NodeGetterSourceGenerationHelper
{
    public const string Attribute =
        @"
namespace NodeGetterGenerators
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class VerifyNodeGettersAttribute : System.Attribute
    {
        public string NodeName { get; }
        
        public VerifyNodeGettersAttribute(string nodeName)
        {
            NodeName = nodeName;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class GenerateNodeGetterAttribute : System.Attribute
    {
        public System.Type NodeType { get; }
        public string NodePath { get; }
        public bool Cache { get; }
        
        public GenerateNodeGetterAttribute(System.Type nodeType, string nodePath, bool cache=false)
        {
            NodeType = nodeType;
            NodePath = nodePath;
            Cache = cache;
        }
    }
}";

    public static string GenerateGetterSurroundings(string className, string[] methodCode)
    {
        var header = $$"""
            #nullable enable
            using System.Collections.Generic;
            namespace {{GetNamespace(className)}};
            public partial class {{GetClassNameWithoutNamespace(className)}} {
            """;
        const string footer = "}";
        var sb = new StringBuilder();
        sb.AppendLine(header);
        foreach (var method in methodCode)
            sb.AppendLine(method);
        sb.AppendLine(footer);
        return sb.ToString();
    }

    public static string GenerateGetterMethod(GetterToGenerate getterToGenerate)
    {
        if (getterToGenerate.IsCached)
            return $"""
                        private {getterToGenerate.NodeType}? _cache{getterToGenerate.MethodName};
                        private {getterToGenerate.NodeType} {getterToGenerate.MethodName}() =>
                            _cache{getterToGenerate.MethodName} ??=
                                GetNode<{getterToGenerate.NodeType}>("{getterToGenerate.NodePath}") 
                                ?? throw new KeyNotFoundException(
                                    "Could not find node '{getterToGenerate.NodePath}' of type '{getterToGenerate.NodeType}'"
                                );
                """;
        return $"""
                    private {getterToGenerate.NodeType} {getterToGenerate.MethodName}() =>
                        GetNode<{getterToGenerate.NodeType}>("{getterToGenerate.NodePath}") 
                        ?? throw new KeyNotFoundException(
                            "Could not find node '{getterToGenerate.NodePath}' of type '{getterToGenerate.NodeType}'"
                        );
            """;
    }

    private static string GetNamespace(string fullClassName)
    {
        var lastDot = fullClassName.LastIndexOf('.');
        return lastDot > 0 ? fullClassName.Substring(0, lastDot) : string.Empty;
    }

    private static string GetClassNameWithoutNamespace(string fullClassName)
    {
        var lastDot = fullClassName.LastIndexOf('.');
        return lastDot > 0 ? fullClassName.Substring(lastDot + 1) : fullClassName;
    }
}

[Generator]
public class NodeGetterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterPostInitializationOutput(static ctx =>
            ctx.AddSource(
                "GenerateNodeGetterAttribute.g.cs",
                SourceText.From(NodeGetterSourceGenerationHelper.Attribute, Encoding.UTF8)
            )
        );

        // Create a provider to recognize syntax where the attribute is applied
        var gettersToGenerate = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
            .Where(static m => m.Item1 is not null && m.Item1.Length > 0);

        // Load additional files
        var tscnFiles = context
            .AdditionalTextsProvider.Select(
                (text, cancellationToken) => text.GetText(cancellationToken)!.ToString()
            )
            .Select((content, _) => ExtractNodeInfoFromTscn(content))
            .Collect()
            .Select((info, _) => info.ToImmutableDictionary(i => i.Item1, i => i.Item2));

        // Register the output source generation
        context.RegisterSourceOutput(
            gettersToGenerate.Combine(tscnFiles),
            static (spc, source) => Execute(source, spc)
        );
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    private static (GetterToGenerate[], GenerationVerification?) GetSemanticTargetForGeneration(
        GeneratorSyntaxContext context
    )
    {
        // We know the node is a ClassDeclarationSyntax thanks to IsSyntaxTargetForGeneration
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        // Get the semantic model for the syntax node
        var semanticModel = context.SemanticModel;

        // Get the symbol for the class declaration
        if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not { } classSymbol)
        {
            return ([], null);
        }

        // Loop through all attributes on the class
        var result = new List<GetterToGenerate>();
        GenerationVerification? verifier = null;
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                var getter = GetterToGenerate.FromAttribute(
                    semanticModel,
                    classSymbol,
                    attributeSyntax
                );
                verifier ??= GenerationVerification.FromAttribute(
                    semanticModel,
                    classSymbol,
                    attributeSyntax
                );
                if (getter != null)
                    result.Add(getter.Value);
            }
        }

        return (result.ToArray(), verifier);
    }

    private static readonly DiagnosticDescriptor InvalidNoteGettersDescriptor = new(
        id: "GODOTNODEGETTERGEN01",
        title: "Invalid Node getters requested",
        messageFormat: "Could not find a node with path '{1}' within scene '{0}'",
        category: "GodotNodeGetter",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static void Execute(
        (
            (GetterToGenerate[], GenerationVerification?),
            ImmutableDictionary<string, TscnNode[]>
        ) source,
        SourceProductionContext context
    )
    {
        var gettersToGenerate = source.Item1.Item1;
        var generationVerification = source.Item1.Item2;
        var nodeInfoMap = source.Item2;

        var targetClassName = gettersToGenerate[0].ClassName;
        var methodCodes = new List<string>();
        foreach (var getterToGenerate in gettersToGenerate)
        {
            var code = NodeGetterSourceGenerationHelper.GenerateGetterMethod(getterToGenerate);
            methodCodes.Add(code);

            // Verify nodes
            if (generationVerification == null)
                continue;
            var verification = generationVerification.Value;

            var verificationNameSplit = verification.NodeName.Split('/');

            var nodes = nodeInfoMap[verificationNameSplit.First()];
            var getterFullNodePath = string.Join(
                "/",
                verificationNameSplit.Skip(1).Concat([getterToGenerate.NodePath])
            );
            var matchingNode = nodes.FirstOrDefault(node =>
                node.GetPath(nodes) == getterFullNodePath
            );
            // TODO support .. and $ in path
            // TODO verify type

            if (matchingNode == null)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        InvalidNoteGettersDescriptor,
                        Location.None,
                        verification.NodeName,
                        getterToGenerate.NodePath
                    )
                );
            }
        }

        var result = NodeGetterSourceGenerationHelper.GenerateGetterSurroundings(
            targetClassName,
            methodCodes.ToArray()
        );
        context.AddSource(
            $"NodeGetter.{targetClassName}.g.cs",
            SourceText.From(result, Encoding.UTF8)
        );
    }

    private static (string, TscnNode[]) ExtractNodeInfoFromTscn(string tscnContent)
    {
        // Collect all lines that define a node from the TSCN file.
        var regex = new Regex(@"\s*(\[\s*node.*\])\s*");
        var matches = regex.Matches(tscnContent);
        var nodeLines = new List<string>();
        for (var i = 0; i < matches.Count; i++)
        {
            nodeLines.Add(matches[i].Groups[1].Value);
        }

        // Collect the specific key-value pairs for each node.
        var kvRegex = new Regex(@"([\w_]+)=""([^""]*)""");
        var nodes = nodeLines
            .Select(line =>
            {
                var matches = kvRegex.Matches(line);
                var nodes = new List<(string, string)>();
                for (var i = 0; i < matches.Count; i++)
                {
                    var groups = matches[i].Groups;
                    nodes.Add((groups[1].Value, groups[2].Value));
                }

                return nodes.ToImmutableDictionary(kv => kv.Item1, kv => kv.Item2);
            })
            .Select(dict => new TscnNode(
                dict["name"],
                dict.GetValueOrDefault("type", "PackedScene"),
                dict.GetValueOrDefault("parent")
            ))
            .ToArray();
        return nodeLines.ToArray().Length == 0 ? ("", []) : (nodes[0].Name, nodes);
    }
}

internal record TscnNode(string Name, string Type, string? Parent)
{
    public readonly string Name = Name;
    public readonly string Type = Type;
    public readonly string? Parent = Parent;

    public string GetPath(TscnNode[] siblings)
    {
        if (Parent is null or ".")
            return Name;

        var parentNode = siblings.First(node => node.Name == Parent);
        if (parentNode == null)
            throw new Exception($"Could not find parent node {Parent}");
        return $"{parentNode.GetPath(siblings)}/{Name}";
    }
}

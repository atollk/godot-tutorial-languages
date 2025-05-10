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

public readonly record struct PropertyGetterToGenerate(
    string PropertyName,
    string PropertyType,
    string NodePath,
    bool IsCached,
    string ClassName,
    string AccessModifier
)
{
    public readonly string PropertyName = PropertyName;
    public readonly string PropertyType = PropertyType;
    public readonly string NodePath = NodePath;
    public readonly bool IsCached = IsCached;
    public readonly string ClassName = ClassName;
    public readonly string AccessModifier = AccessModifier;

    public string BackingFieldName =>
        $"_{char.ToLowerInvariant(PropertyName[0])}{PropertyName.Substring(1)}";

    public static PropertyGetterToGenerate? FromPropertyNode(
        SemanticModel semanticModel,
        INamedTypeSymbol classSymbol,
        PropertyDeclarationSyntax propertySyntax
    )
    {
        // Check if property has a Node attribute
        var nodeAttribute = propertySyntax
            .AttributeLists.SelectMany(al => al.Attributes)
            .FirstOrDefault(a => IsNodeAttribute(semanticModel, a));

        if (nodeAttribute?.ArgumentList == null)
            return null;

        // Get the nodePath from the attribute
        if (nodeAttribute.ArgumentList?.Arguments.Count < 1)
            return null;

        if (
            semanticModel
                .GetConstantValue(nodeAttribute.ArgumentList!.Arguments[0].Expression)
                .Value
            is not string nodePath
        )
            return null;

        // Get caching option (optional second parameter)
        var isCached = false;
        if (nodeAttribute.ArgumentList.Arguments.Count > 1)
        {
            var isCachedOpt = semanticModel
                .GetConstantValue(nodeAttribute.ArgumentList.Arguments[1].Expression)
                .Value;
            if (isCachedOpt is bool x)
                isCached = x;
        }

        // Get property type and name
        var propertyName = propertySyntax.Identifier.Text;

        // Get access modifier
        var accessModifier = "private"; // Default
        if (propertySyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            accessModifier = "public";
        else if (propertySyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
            accessModifier = "protected";
        else if (propertySyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)))
            accessModifier = "internal";

        // Get fully qualified type name
        var typeInfo = semanticModel.GetTypeInfo(propertySyntax.Type);
        var propertyType = typeInfo.Type?.ToDisplayString() ?? propertySyntax.Type.ToString();

        return new PropertyGetterToGenerate(
            propertyName,
            propertyType,
            nodePath,
            isCached,
            classSymbol.ToDisplayString(),
            accessModifier
        );
    }

    private static bool IsNodeAttribute(SemanticModel semanticModel, AttributeSyntax attribute)
    {
        if (semanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
            return false;

        var fullName = attributeSymbol.ContainingType.ToDisplayString();
        return fullName == "NodeGetterGenerators.NodeAttribute";
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

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class NodeAttribute : System.Attribute
    {
        public string NodePath { get; }
        public bool Cache { get; }
        
        public NodeAttribute(string nodePath, bool cache = false)
        {
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

    public static string GeneratePropertyGetter(PropertyGetterToGenerate getterToGenerate)
    {
        // Use the fully qualified type to ensure it's properly resolved
        var fullTypeName = getterToGenerate.PropertyType;

        return getterToGenerate.IsCached
            ? $"""
                    private {fullTypeName}? {getterToGenerate.BackingFieldName};
                    {getterToGenerate.AccessModifier} partial {fullTypeName} {getterToGenerate.PropertyName} => 
                        {getterToGenerate.BackingFieldName} ??= 
                            GetNode<{fullTypeName}>("{getterToGenerate.NodePath}") 
                            ?? throw new KeyNotFoundException(
                                "Could not find node '{getterToGenerate.NodePath}' of type '{fullTypeName}'"
                            );
                """
            : $"""
                    {getterToGenerate.AccessModifier} partial {fullTypeName} {getterToGenerate.PropertyName} => 
                        GetNode<{fullTypeName}>("{getterToGenerate.NodePath}") 
                        ?? throw new KeyNotFoundException(
                            "Could not find node '{getterToGenerate.NodePath}' of type '{fullTypeName}'"
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
                "NodeAttributes.g.cs",
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
        node
            is ClassDeclarationSyntax { AttributeLists.Count: > 0 }
                or ClassDeclarationSyntax { Members: { Count: > 0 } };

    private static (
        PropertyGetterToGenerate[],
        GenerationVerification?
    ) GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
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

        // Find verify node attribute
        GenerationVerification? verifier = null;
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                verifier ??= GenerationVerification.FromAttribute(
                    semanticModel,
                    classSymbol,
                    attributeSyntax
                );
            }
        }

        // Find properties with Node attribute
        var result = new List<PropertyGetterToGenerate>();
        foreach (var member in classDeclarationSyntax.Members)
        {
            if (member is not PropertyDeclarationSyntax propertySyntax)
                continue;

            var propertyGetter = PropertyGetterToGenerate.FromPropertyNode(
                semanticModel,
                classSymbol,
                propertySyntax
            );

            if (propertyGetter != null)
                result.Add(propertyGetter.Value);
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
            (PropertyGetterToGenerate[], GenerationVerification?),
            ImmutableDictionary<string, TscnNode[]>
        ) source,
        SourceProductionContext context
    )
    {
        var gettersToGenerate = source.Item1.Item1;
        var generationVerification = source.Item1.Item2;
        var nodeInfoMap = source.Item2;

        if (gettersToGenerate.Length == 0)
            return;

        var targetClassName = gettersToGenerate[0].ClassName;
        var methodCodes = new List<string>();
        foreach (var getterToGenerate in gettersToGenerate)
        {
            var code = NodeGetterSourceGenerationHelper.GeneratePropertyGetter(getterToGenerate);
            methodCodes.Add(code);

            // Verify nodes
            if (generationVerification == null)
                continue;
            var verification = generationVerification.Value;

            var verificationNameSplit = verification.NodeName.Split('/');

            if (!nodeInfoMap.ContainsKey(verificationNameSplit.First()))
                continue;

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

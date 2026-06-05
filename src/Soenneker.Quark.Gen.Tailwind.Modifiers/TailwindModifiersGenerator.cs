using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Soenneker.Quark.Gen.Tailwind.Modifiers;

/// <summary>
/// Represents the tailwind modifiers generator.
/// </summary>
[Generator]
public sealed class TailwindModifiersGenerator : IIncrementalGenerator
{
    private const string _attributeMetadataName = "Soenneker.Quark.TailwindModifiersAttribute";

    private static readonly ModifierProperty[] _modifierProperties =
    {
        new("OnSm", "sm"),
        new("OnMd", "md"),
        new("OnLg", "lg"),
        new("OnXl", "xl"),
        new("On2xl", "2xl"),
        new("OnMaxSm", "max-sm"),
        new("OnMaxMd", "max-md"),
        new("OnMaxLg", "max-lg"),
        new("OnMaxXl", "max-xl"),
        new("OnContainerSm", "@sm"),
        new("OnContainerMd", "@md"),
        new("OnContainerLg", "@lg"),
        new("OnContainerXl", "@xl"),
        new("OnContainer2xl", "@2xl"),
        new("OnContainerMaxSm", "@max-sm"),
        new("OnContainerMaxMd", "@max-md"),
        new("OnContainer", "@container"),
        new("OnContainerNormal", "@container-normal"),
        new("OnHover", "hover"),
        new("OnFocus", "focus"),
        new("OnFocusVisible", "focus-visible"),
        new("OnFocusWithin", "focus-within"),
        new("OnActive", "active"),
        new("OnVisited", "visited"),
        new("OnTarget", "target"),
        new("OnOpen", "open"),
        new("OnDisabled", "disabled"),
        new("OnEnabled", "enabled"),
        new("OnChecked", "checked"),
        new("OnIndeterminate", "indeterminate"),
        new("OnDefault", "default"),
        new("OnRequired", "required"),
        new("OnOptional", "optional"),
        new("OnValid", "valid"),
        new("OnInvalid", "invalid"),
        new("OnInRange", "in-range"),
        new("OnOutOfRange", "out-of-range"),
        new("OnPlaceholderShown", "placeholder-shown"),
        new("OnReadOnly", "read-only"),
        new("OnReadWrite", "read-write"),
        new("OnAutofill", "autofill"),
        new("OnMotionSafe", "motion-safe"),
        new("OnMotionReduce", "motion-reduce"),
        new("OnContrastMore", "contrast-more"),
        new("OnContrastLess", "contrast-less"),
        new("OnForcedColors", "forced-colors"),
        new("OnPortrait", "portrait"),
        new("OnLandscape", "landscape"),
        new("OnPrint", "print"),
        new("OnRtl", "rtl"),
        new("OnLtr", "ltr"),
        new("OnDark", "dark"),
        new("OnFirst", "first"),
        new("OnLast", "last"),
        new("OnOnly", "only"),
        new("OnOdd", "odd"),
        new("OnEven", "even"),
        new("OnEmpty", "empty"),
        new("OnBefore", "before"),
        new("OnAfter", "after"),
        new("OnPlaceholder", "placeholder"),
        new("OnFile", "file"),
        new("OnMarker", "marker"),
        new("OnSelection", "selection"),
        new("OnFirstLetter", "first-letter"),
        new("OnFirstLine", "first-line"),
        new("OnBackdrop", "backdrop"),
        new("OnGroupHover", "group-hover"),
        new("OnGroupFocus", "group-focus"),
        new("OnGroupFocusVisible", "group-focus-visible"),
        new("OnGroupActive", "group-active"),
        new("OnGroupVisited", "group-visited"),
        new("OnGroupDisabled", "group-disabled"),
        new("OnGroupChecked", "group-checked"),
        new("OnGroupOpen", "group-open"),
        new("OnPeerHover", "peer-hover"),
        new("OnPeerFocus", "peer-focus"),
        new("OnPeerFocusVisible", "peer-focus-visible"),
        new("OnPeerActive", "peer-active"),
        new("OnPeerDisabled", "peer-disabled"),
        new("OnPeerChecked", "peer-checked"),
        new("OnPeerInvalid", "peer-invalid"),
        new("OnPeerRequired", "peer-required"),
        new("OnPeerPlaceholderShown", "peer-placeholder-shown"),
        new("OnPeerOpen", "peer-open"),
        new("OnAriaChecked", "aria-checked"),
        new("OnAriaDisabled", "aria-disabled"),
        new("OnAriaExpanded", "aria-expanded"),
        new("OnAriaHidden", "aria-hidden"),
        new("OnAriaPressed", "aria-pressed"),
        new("OnAriaReadonly", "aria-readonly"),
        new("OnAriaRequired", "aria-required"),
        new("OnAriaSelected", "aria-selected")
    };

    private static readonly PaletteProperty[] PaletteProperties =
    {
        new("Slate"),
        new("Gray"),
        new("Zinc"),
        new("Neutral"),
        new("Stone"),
        new("Red"),
        new("Orange"),
        new("Amber"),
        new("Yellow"),
        new("Lime"),
        new("Green"),
        new("Emerald"),
        new("Teal"),
        new("Cyan"),
        new("Sky"),
        new("Blue"),
        new("Indigo"),
        new("Violet"),
        new("Purple"),
        new("Fuchsia"),
        new("Pink"),
        new("Rose")
    };

    /// <summary>
    /// Executes the initialize operation.
    /// </summary>
    /// <param name="context">The context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx => ctx.AddSource("TailwindModifiersAttribute.g.cs", AttributeSource));

        IncrementalValuesProvider<ModifierCandidate?> candidates = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
            static (ctx, _) => GetModifierCandidate(ctx))
            .Where(static candidate => candidate is not null);

        context.RegisterSourceOutput(candidates.Collect(), static (ctx, candidates) =>
        {
            ImmutableArray<ModifierCandidate> modifiers = candidates.Where(static candidate => candidate is not null)
                                                                     .Select(static candidate => candidate!.Value)
                                                                     .Distinct()
                                                                     .OrderBy(static candidate => candidate.FullTypeName, StringComparer.Ordinal)
                                                                     .ToImmutableArray();

            foreach (ModifierCandidate modifier in modifiers)
            {
                string hintName = modifier.FullTypeName.Replace("global::", string.Empty)
                                      .Replace(".", "_")
                                      .Replace("+", "_") + ".TailwindModifiers.g.cs";

                ctx.AddSource(hintName, GenerateModifierSource(modifier));
            }
        });
    }

    private static ModifierCandidate? GetModifierCandidate(GeneratorSyntaxContext context)
    {
        var declaration = (ClassDeclarationSyntax) context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol typeSymbol)
            return null;

        AttributeData? attribute = null;

        foreach (AttributeData attr in typeSymbol.GetAttributes())
        {
            INamedTypeSymbol? attrClass = attr.AttributeClass;

            if (attrClass is null)
                continue;

            if (string.Equals(attrClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "global::" + _attributeMetadataName, StringComparison.Ordinal) ||
                string.Equals(attrClass.ToDisplayString(), _attributeMetadataName, StringComparison.Ordinal))
            {
                attribute = attr;
                break;
            }
        }

        if (attribute is null || attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol builderType)
            return null;

        string? ns = typeSymbol.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
            ? containingNamespace.ToDisplayString()
            : null;

        var containingTypes = new Stack<string>();
        INamedTypeSymbol? containingType = typeSymbol.ContainingType;

        while (containingType is not null)
        {
            containingTypes.Push(containingType.Name);
            containingType = containingType.ContainingType;
        }

        var includeColorPalettes = false;

        foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
        {
            if (string.Equals(namedArgument.Key, "IncludeColorPalettes", StringComparison.Ordinal) &&
                namedArgument.Value.Value is bool value)
            {
                includeColorPalettes = value;
                break;
            }
        }

        return new ModifierCandidate(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            builderType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ns,
            [..containingTypes],
            includeColorPalettes);
    }

    private static string GenerateModifierSource(ModifierCandidate candidate)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (candidate.Namespace is { Length: > 0 })
        {
            sb.Append("namespace ");
            sb.Append(candidate.Namespace);
            sb.AppendLine(";");
            sb.AppendLine();
        }

        for (var i = 0; i < candidate.ContainingTypes.Length; i++)
        {
            sb.Append("partial class ");
            sb.Append(candidate.ContainingTypes[i]);
            sb.AppendLine();
            sb.AppendLine("{");
        }

        sb.Append("public static partial class ");
        sb.Append(candidate.TypeName);
        sb.AppendLine();
        sb.AppendLine("{");

        for (var i = 0; i < _modifierProperties.Length; i++)
        {
            ModifierProperty property = _modifierProperties[i];
            sb.Append("    public static ");
            sb.Append(candidate.BuilderTypeName);
            sb.Append(' ');
            sb.Append(property.Name);
            sb.Append(" => new ");
            sb.Append(candidate.BuilderTypeName);
            sb.Append("().Modifier(\"");
            sb.Append(property.Modifier);
            sb.AppendLine("\");");
        }

        if (candidate.IncludeColorPalettes)
        {
            sb.AppendLine();

            for (var i = 0; i < PaletteProperties.Length; i++)
            {
                PaletteProperty property = PaletteProperties[i];
                sb.Append("    public static global::Soenneker.Quark.ColorPaletteBuilder<");
                sb.Append(candidate.BuilderTypeName);
                sb.Append("> ");
                sb.Append(property.Name);
                sb.Append(" => new(global::Soenneker.Quark.ColorPaletteEnum.");
                sb.Append(property.Name);
                sb.Append(", static token => new ");
                sb.Append(candidate.BuilderTypeName);
                sb.AppendLine("().Token(token));");
            }
        }

        sb.AppendLine("}");

        for (var i = 0; i < candidate.ContainingTypes.Length; i++)
            sb.AppendLine("}");

        return sb.ToString();
    }

    private const string AttributeSource = """
// <auto-generated/>
#nullable enable

namespace Soenneker.Quark;

[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class TailwindModifiersAttribute : global::System.Attribute
{
    public TailwindModifiersAttribute(global::System.Type builderType)
    {
        BuilderType = builderType;
    }

    public global::System.Type BuilderType { get; }

    public bool IncludeColorPalettes { get; init; }
}
""";

    private readonly struct ModifierProperty
    {
        public ModifierProperty(string name, string modifier)
        {
            Name = name;
            Modifier = modifier;
        }

        public string Name { get; }
        public string Modifier { get; }
    }

    private readonly struct ModifierCandidate : IEquatable<ModifierCandidate>
    {
        public ModifierCandidate(string typeName, string fullTypeName, string builderTypeName, string? ns, ImmutableArray<string> containingTypes, bool includeColorPalettes)
        {
            TypeName = typeName;
            FullTypeName = fullTypeName;
            BuilderTypeName = builderTypeName;
            Namespace = ns;
            ContainingTypes = containingTypes;
            IncludeColorPalettes = includeColorPalettes;
        }

        public string TypeName { get; }
        public string FullTypeName { get; }
        public string BuilderTypeName { get; }
        public string? Namespace { get; }
        public ImmutableArray<string> ContainingTypes { get; }
        public bool IncludeColorPalettes { get; }

        public bool Equals(ModifierCandidate other) =>
            string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) &&
            string.Equals(FullTypeName, other.FullTypeName, StringComparison.Ordinal) &&
            string.Equals(BuilderTypeName, other.BuilderTypeName, StringComparison.Ordinal) &&
            string.Equals(Namespace, other.Namespace, StringComparison.Ordinal) &&
            ContainingTypes.SequenceEqual(other.ContainingTypes) &&
            IncludeColorPalettes == other.IncludeColorPalettes;

        public override bool Equals(object? obj) => obj is ModifierCandidate other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(TypeName);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(FullTypeName);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(BuilderTypeName);
                hash = (hash * 31) + (Namespace is null ? 0 : StringComparer.Ordinal.GetHashCode(Namespace));

                for (var i = 0; i < ContainingTypes.Length; i++)
                    hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ContainingTypes[i]);

                hash = (hash * 31) + IncludeColorPalettes.GetHashCode();

                return hash;
            }
        }
    }

    private readonly struct PaletteProperty
    {
        public PaletteProperty(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}

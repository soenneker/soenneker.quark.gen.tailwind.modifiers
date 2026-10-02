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
/// Generates Tailwind modifier and optional color-palette entry points for attributed Quark builder classes.
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
        new("Rose"),
        new("Mauve"),
        new("Olive"),
        new("Mist"),
        new("Taupe")
    };

    /// <summary>
    /// Registers attribute emission, target discovery, and modifier source generation.
    /// </summary>
    /// <param name="context">The incremental generator initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx => ctx.AddSource("TailwindModifiersAttribute.g.cs", AttributeSource));

        IncrementalValuesProvider<ModifierCandidate> candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            _attributeMetadataName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (ctx, _) => GetModifierCandidate(ctx))
            .Where(static candidate => candidate is not null)
            .Select(static (candidate, _) => candidate!.Value);

        context.RegisterSourceOutput(candidates, static (ctx, modifier) =>
        {
            string hintName = modifier.FullTypeName.Replace("global::", string.Empty)
                                  .Replace(".", "_")
                                  .Replace("+", "_") + ".TailwindModifiers.g.cs";

            ctx.AddSource(hintName, GenerateModifierSource(modifier));
        });
    }

    private static ModifierCandidate? GetModifierCandidate(GeneratorAttributeSyntaxContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;
        AttributeData attribute = context.Attributes[0];

        if (attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol builderType)
            return null;

        string? ns = typeSymbol.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
            ? containingNamespace.ToDisplayString()
            : null;

        Stack<string>? containingTypes = null;
        INamedTypeSymbol? containingType = typeSymbol.ContainingType;

        while (containingType is not null)
        {
            (containingTypes ??= new Stack<string>()).Push(containingType.Name);
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
            containingTypes is null ? ImmutableArray<string>.Empty : [..containingTypes],
            includeColorPalettes);
    }

    private static string GenerateModifierSource(ModifierCandidate candidate)
    {
        var sb = new StringBuilder(candidate.IncludeColorPalettes ? 24576 : 16384);
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
                sb.Append(" => new(\"");
                sb.Append(property.Token);
                sb.Append("\", static token => new ");
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

}

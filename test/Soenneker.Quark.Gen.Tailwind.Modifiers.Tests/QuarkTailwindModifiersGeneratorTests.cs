using Soenneker.Tests.Unit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;

namespace Soenneker.Quark.Gen.Tailwind.Modifiers.Tests;

public sealed class QuarkTailwindModifiersGeneratorTests : UnitTest
{
    [Test]
    public void Generates_requested_tailwind_modifier_entrypoints()
    {
        const string source = """
namespace Soenneker.Quark;

public abstract class CssBuilderBase<TBuilder> where TBuilder : CssBuilderBase<TBuilder>
{
    public TBuilder Modifier(string modifier) => (TBuilder)this;
}

public sealed class DemoBuilder : CssBuilderBase<DemoBuilder>
{
    internal DemoBuilder()
    {
    }
}

[TailwindModifiers(typeof(DemoBuilder))]
public static partial class Demo
{
}
""";

        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new TailwindModifiersGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        GeneratorDriverRunResult result = driver.GetRunResult();
        string generated = string.Join("\n", result.GeneratedTrees.Select(static tree => tree.GetText().ToString()));

        AssertContains(generated, "public static global::Soenneker.Quark.DemoBuilder OnSm => new global::Soenneker.Quark.DemoBuilder().Modifier(\"sm\");");
        AssertContains(generated, "public static global::Soenneker.Quark.DemoBuilder OnMaxMd => new global::Soenneker.Quark.DemoBuilder().Modifier(\"max-md\");");
        AssertContains(generated, "public static global::Soenneker.Quark.DemoBuilder OnContainerSm => new global::Soenneker.Quark.DemoBuilder().Modifier(\"@sm\");");
        AssertContains(generated, "public static global::Soenneker.Quark.DemoBuilder OnFocusWithin => new global::Soenneker.Quark.DemoBuilder().Modifier(\"focus-within\");");
        AssertContains(generated, "public static global::Soenneker.Quark.DemoBuilder OnMotionReduce => new global::Soenneker.Quark.DemoBuilder().Modifier(\"motion-reduce\");");
        AssertContains(generated, "public static global::Soenneker.Quark.DemoBuilder OnPeerPlaceholderShown => new global::Soenneker.Quark.DemoBuilder().Modifier(\"peer-placeholder-shown\");");
        AssertContains(generated, "public static global::Soenneker.Quark.DemoBuilder OnAriaSelected => new global::Soenneker.Quark.DemoBuilder().Modifier(\"aria-selected\");");
    }

    [Test]
    public void Generates_color_palette_entrypoints_when_requested()
    {
        const string source = """
namespace Soenneker.Quark;

public abstract class CssBuilderBase<TBuilder> where TBuilder : CssBuilderBase<TBuilder>
{
    public TBuilder Modifier(string modifier) => (TBuilder)this;
}

public sealed class DemoBuilder : CssBuilderBase<DemoBuilder>
{
    internal DemoBuilder()
    {
    }

    public DemoBuilder Token(string token) => this;
}

[TailwindModifiers(typeof(DemoBuilder), IncludeColorPalettes = true)]
public static partial class Demo
{
}
""";

        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new TailwindModifiersGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        GeneratorDriverRunResult result = driver.GetRunResult();
        string generated = string.Join("\n", result.GeneratedTrees.Select(static tree => tree.GetText().ToString()));

        AssertContains(generated, "public static global::Soenneker.Quark.ColorPaletteBuilder<global::Soenneker.Quark.DemoBuilder> Slate => new(\"slate\", static token => new global::Soenneker.Quark.DemoBuilder().Token(token));");
        AssertContains(generated, "public static global::Soenneker.Quark.ColorPaletteBuilder<global::Soenneker.Quark.DemoBuilder> Neutral => new(\"neutral\", static token => new global::Soenneker.Quark.DemoBuilder().Token(token));");
        AssertContains(generated, "public static global::Soenneker.Quark.ColorPaletteBuilder<global::Soenneker.Quark.DemoBuilder> Rose => new(\"rose\", static token => new global::Soenneker.Quark.DemoBuilder().Token(token));");
        AssertContains(generated, "Mauve => new(\"mauve\", static token => new global::Soenneker.Quark.DemoBuilder().Token(token));");
        AssertContains(generated, "Olive => new(\"olive\", static token => new global::Soenneker.Quark.DemoBuilder().Token(token));");
        AssertContains(generated, "Mist => new(\"mist\", static token => new global::Soenneker.Quark.DemoBuilder().Token(token));");
        AssertContains(generated, "Taupe => new(\"taupe\", static token => new global::Soenneker.Quark.DemoBuilder().Token(token));");
    }

    [Test]
    public void Container_declarations_are_not_generated_as_variants()
    {
        const string source = "namespace Soenneker.Quark; public class DemoBuilder {} [TailwindModifiers(typeof(DemoBuilder))] public static partial class Demo {}";
        var compilation = CSharpCompilation.Create("ContainerTests", [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TailwindModifiersGenerator()).RunGenerators(compilation);
        string generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(tree => tree.ToString()));
        if (generated.Contains(" OnContainer =>", StringComparison.Ordinal) || generated.Contains(" OnContainerNormal =>", StringComparison.Ordinal))
            throw new InvalidOperationException("Container declarations must not be generated as query variants.");
    }

    private static void AssertContains(string source, string expected)
    {
        if (!source.Contains(expected, StringComparison.Ordinal))
            throw new InvalidOperationException("Generated source did not contain: " + expected);
    }
}

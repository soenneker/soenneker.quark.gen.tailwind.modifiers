using Soenneker.Tests.Unit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;
using System.IO;

namespace Soenneker.Quark.Gen.Tailwind.Modifiers.Tests;

public sealed class QuarkTailwindModifiersGeneratorTests : UnitTest
{
    [Test]
    public void Static_modifier_compiles_and_forwards_arbitrary_selector_to_a_new_builder()
    {
        const string source = """
namespace Soenneker.Quark;

public sealed class DemoBuilder
{
    private string _modifier = "";
    internal DemoBuilder() { }
    public DemoBuilder Modifier(string modifier) { _modifier = modifier; return this; }
    public string Token(string token) => _modifier + ":min-h-" + token;
}

[TailwindModifiers(typeof(DemoBuilder))]
public static partial class Demo
{
    public static string Verify()
    {
        var first = Modifier("[&_[data-slot=tag-input-inline-container]]");
        var second = Modifier("hover");
        return first.Token("7") + "|" + second.Token("0");
    }
}
""";

        var compilation = CSharpCompilation.Create(
            "StaticModifierTests",
            [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TailwindModifiersGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out var diagnostics);
        if (diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            throw new InvalidOperationException(string.Join("\n", diagnostics));

        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        if (!result.Success)
            throw new InvalidOperationException(string.Join("\n", result.Diagnostics));

        var assembly = System.Reflection.Assembly.Load(stream.ToArray());
        object? actual = assembly.GetType("Soenneker.Quark.Demo")!.GetMethod("Verify")!.Invoke(null, null);
        if (!Equals(actual, "[&_[data-slot=tag-input-inline-container]]:min-h-7|hover:min-h-0"))
            throw new InvalidOperationException("Static modifiers must preserve selectors and use independent builders: " + actual);
    }

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

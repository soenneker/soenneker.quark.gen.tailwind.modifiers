[![](https://img.shields.io/nuget/v/soenneker.quark.gen.tailwind.modifiers.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.quark.gen.tailwind.modifiers/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.quark.gen.tailwind.modifiers/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.quark.gen.tailwind.modifiers/actions/workflows/publish-package.yml)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.quark.gen.tailwind.modifiers/build-and-test.yml?label=Build&style=for-the-badge)](https://github.com/soenneker/soenneker.quark.gen.tailwind.modifiers/actions/workflows/build-and-test.yml)
[![](https://img.shields.io/nuget/dt/soenneker.quark.gen.tailwind.modifiers.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.quark.gen.tailwind.modifiers/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.quark.gen.tailwind.modifiers/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.quark.gen.tailwind.modifiers/actions/workflows/codeql.yml)

# Soenneker.Quark.Gen.Tailwind.Modifiers

Generates Tailwind variant entry points for Quark fluent builder APIs.

This is a build-time package for authors of Quark builder libraries. Applications consuming `Soenneker.Quark.Builders` already receive the generated APIs and do not need to install it directly.

## Install

```bash
dotnet add package Soenneker.Quark.Gen.Tailwind.Modifiers
```

## Usage

Annotate a top-level `public static partial` entry-point class with its builder type:

```csharp
[TailwindModifiers(typeof(BackgroundColorBuilder))]
public static partial class BackgroundColor
{
}
```

The generator adds modifier properties that create the builder and call its `Modifier(string)` method:

```csharp
BackgroundColor.OnHover
BackgroundColor.OnFocusVisible
BackgroundColor.OnDark
BackgroundColor.OnMd
BackgroundColor.OnGroupHover
BackgroundColor.OnAriaExpanded
```

Each property returns a fresh `BackgroundColorBuilder` with the corresponding modifier applied, ready for the builder’s normal value properties or methods (for example, `BackgroundColor.OnHover.Primary`).

Generated variants cover responsive and max-width breakpoints, container queries, interaction and form states, media preferences, direction and dark mode, structural pseudo-classes, pseudo-elements, group and peer state, and common ARIA states.

## Color palettes

Enable palette entry points for builders that expose `Token(string)` and use Quark’s color palette types:

```csharp
[TailwindModifiers(typeof(BackgroundColorBuilder), IncludeColorPalettes = true)]
public static partial class BackgroundColor
{
}

var color = BackgroundColor.Blue.Is600;
```

The target class must be partial because the generated properties extend it. The builder must be constructible from the generated code and support the methods required by the selected options.

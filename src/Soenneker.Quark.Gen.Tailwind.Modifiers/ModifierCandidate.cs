using System;
using System.Collections.Immutable;

namespace Soenneker.Quark.Gen.Tailwind.Modifiers;

internal readonly struct ModifierCandidate : IEquatable<ModifierCandidate>
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
        ContainingTypesEqual(ContainingTypes, other.ContainingTypes) &&
        IncludeColorPalettes == other.IncludeColorPalettes;

    private static bool ContainingTypesEqual(ImmutableArray<string> left, ImmutableArray<string> right)
    {
        if (left.Length != right.Length)
            return false;
        for (var i = 0; i < left.Length; i++)
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                return false;
        return true;
    }

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

namespace Soenneker.Quark.Gen.Tailwind.Modifiers;

internal readonly struct PaletteProperty
{
    public PaletteProperty(string name)
    {
        Name = name;
        Token = name.ToLowerInvariant();
    }

    public string Name { get; }
    public string Token { get; }
}

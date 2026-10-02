namespace Soenneker.Quark.Gen.Tailwind.Modifiers;

internal readonly struct ModifierProperty
{
    public ModifierProperty(string name, string modifier)
    {
        Name = name;
        Modifier = modifier;
    }

    public string Name { get; }
    public string Modifier { get; }
}

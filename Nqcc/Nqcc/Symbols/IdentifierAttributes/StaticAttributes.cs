namespace Nqcc.Symbols.IdentifierAttributes;

public class StaticAttributes(InitialValue initialValue, bool global) : IdentifierAttribute
{
    public InitialValue InitialValue { get; } = initialValue;
    public bool Global { get; } = global;
}

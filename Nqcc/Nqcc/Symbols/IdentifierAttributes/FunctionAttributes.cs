namespace Nqcc.Symbols.IdentifierAttributes;

public class FunctionAttributes(bool defined, bool global) : IdentifierAttribute
{
    public bool Defined { get; } = defined;
    public bool Global { get; } = global;
}

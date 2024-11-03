namespace Nqcc.Symbols;

public class Variable(string name, Type type, IdentifierAttribute attributes) : Symbol(name)
{
    public Type Type { get; } = type;
    public IdentifierAttribute Attributes { get; } = attributes;
}

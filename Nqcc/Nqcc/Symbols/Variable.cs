namespace Nqcc.Symbols;

public class Variable(string name, Type type) : Symbol(name)
{
    public Type Type { get; } = type;
}

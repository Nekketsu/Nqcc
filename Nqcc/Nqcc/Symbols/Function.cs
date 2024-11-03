using Nqcc.Symbols.IdentifierAttributes;

namespace Nqcc.Symbols;

public class Function(string name, int parameterCount, bool isDefined, FunctionAttributes attributes, int stackSize = 0) : Symbol(name)
{
    public int ParameterCount { get; } = parameterCount;
    public bool IsDefined { get; } = isDefined;
    public FunctionAttributes Attributes { get; } = attributes;
    public int StackSize { get; } = stackSize;
}

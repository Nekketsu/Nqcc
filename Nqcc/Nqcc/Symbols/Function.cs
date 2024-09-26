namespace Nqcc.Symbols;

public class Function(string name, int parameterCount, bool isDefined, int stackSize = 0) : Symbol(name)
{
    public int ParameterCount { get; } = parameterCount;
    public bool IsDefined { get; } = isDefined;
    public int StackSize { get; } = stackSize;
}

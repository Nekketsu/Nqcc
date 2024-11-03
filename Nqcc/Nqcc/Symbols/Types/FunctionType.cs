namespace Nqcc.Symbols.Types;

public class FunctionType(int parameterCount) : Type
{
    public int ParameterCount { get; } = parameterCount;
}

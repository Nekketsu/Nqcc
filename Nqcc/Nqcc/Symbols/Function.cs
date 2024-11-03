using Nqcc.Symbols.IdentifierAttributes;
using Nqcc.Symbols.Types;

namespace Nqcc.Symbols;

public class Function(string name, FunctionType functionType, FunctionAttributes attributes, int stackSize = 0) : Symbol(name)
{
    public FunctionType FunctionType { get; } = functionType;
    public FunctionAttributes Attributes { get; } = attributes;
    public int StackSize { get; } = stackSize;
}

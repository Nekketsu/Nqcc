using System.Collections.Immutable;

namespace Nqcc.Tacky.Instructions;

public class FunctionCall(string name, ImmutableArray<Operand> arguments, Operand destination) : Instruction
{
    public string Name { get; } = name;
    public ImmutableArray<Operand> Arguments { get; } = arguments;
    public Operand Destination { get; } = destination;
}

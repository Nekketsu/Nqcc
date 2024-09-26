using System.Collections.Immutable;

namespace Nqcc.Assembly;

public class FunctionDefinition(string name, ImmutableArray<Instruction> instructions)
{
    public string Name { get; } = name;
    public ImmutableArray<Instruction> Instructions { get; } = instructions;
}

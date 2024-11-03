using System.Collections.Immutable;

namespace Nqcc.Assembly.TopLevels;

public class Function(string name, bool global, ImmutableArray<Instruction> instructions) : TopLevel
{
    public string Name { get; } = name;
    public bool Global { get; } = global;
    public ImmutableArray<Instruction> Instructions { get; } = instructions;
}

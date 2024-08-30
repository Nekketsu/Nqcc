using System.Collections.Immutable;

namespace Nqcc.Tacky;

public class Function(string name, ImmutableArray<Instruction> body) : TackyNode
{
    public string Name { get; } = name;
    public ImmutableArray<Instruction> Body { get; } = body;
}

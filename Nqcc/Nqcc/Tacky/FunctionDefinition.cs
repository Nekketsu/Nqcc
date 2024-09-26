using System.Collections.Immutable;

namespace Nqcc.Tacky;

public class FunctionDefinition(string name, ImmutableArray<string> parameters, ImmutableArray<Instruction> body) : TackyNode
{
    public string Name { get; } = name;
    public ImmutableArray<string> Parameters { get; } = parameters;
    public ImmutableArray<Instruction> Body { get; } = body;
}

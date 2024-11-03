using System.Collections.Immutable;

namespace Nqcc.Tacky.TopLevels;

public class Function(string name, bool global, ImmutableArray<string> parameters, ImmutableArray<Instruction> body) : TopLevel
{
    public string Name { get; } = name;
    public bool Global { get; } = global;
    public ImmutableArray<string> Parameters { get; } = parameters;
    public ImmutableArray<Instruction> Body { get; } = body;
}

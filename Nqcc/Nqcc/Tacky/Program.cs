using System.Collections.Immutable;

namespace Nqcc.Tacky;

public class Program(ImmutableArray<FunctionDefinition> functionDefinitions) : TackyNode
{
    public ImmutableArray<FunctionDefinition> FunctionDefinitions { get; } = functionDefinitions;
}

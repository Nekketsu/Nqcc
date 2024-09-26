using System.Collections.Immutable;

namespace Nqcc.Assembly;

public class Program(ImmutableArray<FunctionDefinition> functionDefinitions)
{
    public ImmutableArray<FunctionDefinition> FunctionDefinitions { get; } = functionDefinitions;
}

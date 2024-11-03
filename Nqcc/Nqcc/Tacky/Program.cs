using System.Collections.Immutable;

namespace Nqcc.Tacky;

public class Program(ImmutableArray<TopLevel> topLevels) : TackyNode
{
    public ImmutableArray<TopLevel> TopLevels { get; } = topLevels;
}

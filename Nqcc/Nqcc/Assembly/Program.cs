using System.Collections.Immutable;

namespace Nqcc.Assembly;

public class Program(ImmutableArray<TopLevel> topLevels)
{
    public ImmutableArray<TopLevel> TopLevels { get; } = topLevels;
}

public abstract class TopLevel
{
}

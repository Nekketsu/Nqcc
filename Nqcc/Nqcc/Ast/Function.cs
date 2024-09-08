using System.Collections.Immutable;

namespace Nqcc.Ast;

public class Function(string name, ImmutableArray<BlockItem> body) : SyntaxNode
{
    public string Name { get; } = name;
    public ImmutableArray<BlockItem> Body { get; } = body;
}
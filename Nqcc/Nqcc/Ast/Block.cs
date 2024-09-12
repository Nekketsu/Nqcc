using System.Collections.Immutable;

namespace Nqcc.Ast;

public class Block(ImmutableArray<BlockItem> blockItems) : SyntaxNode
{
    public ImmutableArray<BlockItem> BlockItems { get; } = blockItems;
}

using System.Collections.Immutable;

namespace Nqcc.Ast;

public class Program(ImmutableArray<Declaration> declarations) : SyntaxNode
{
    public ImmutableArray<Declaration> Declarations { get; } = declarations;
}
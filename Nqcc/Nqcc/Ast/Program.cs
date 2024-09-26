using Nqcc.Ast.Declarations;
using System.Collections.Immutable;

namespace Nqcc.Ast;

public class Program(ImmutableArray<FunctionDeclaration> functionDeclarations) : SyntaxNode
{
    public ImmutableArray<FunctionDeclaration> FunctionDeclarations { get; } = functionDeclarations;
}
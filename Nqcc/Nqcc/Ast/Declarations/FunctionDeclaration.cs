using System.Collections.Immutable;

namespace Nqcc.Ast.Declarations;

public class FunctionDeclaration(string name, ImmutableArray<string> parameters, Block? body) : Declaration
{
    public string Name { get; } = name;
    public ImmutableArray<string> Parameters { get; } = parameters;
    public Block? Body { get; } = body;
}

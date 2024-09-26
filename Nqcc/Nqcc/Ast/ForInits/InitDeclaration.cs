using Nqcc.Ast.Declarations;

namespace Nqcc.Ast.ForInits;

public class InitDeclaration(VariableDeclaration declaration) : ForInit
{
    public VariableDeclaration Declaration { get; } = declaration;
}

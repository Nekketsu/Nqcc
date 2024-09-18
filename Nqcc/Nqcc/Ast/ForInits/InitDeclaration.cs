namespace Nqcc.Ast.ForInits;

public class InitDeclaration(Declaration declaration) : ForInit
{
    public Declaration Declaration { get; } = declaration;
}

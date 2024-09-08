namespace Nqcc.Ast.BlockItems;

public class Declaration(Ast.Declaration innerDeclaration) : BlockItem
{
    public Ast.Declaration InnerDeclaration { get; } = innerDeclaration;
}

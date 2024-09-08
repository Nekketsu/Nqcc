namespace Nqcc.Ast.BlockItems;

public class Statement(Ast.Statement innerStatement) : BlockItem
{
    public Ast.Statement InnerStatement { get; } = innerStatement;
}

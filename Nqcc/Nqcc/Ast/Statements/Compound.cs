namespace Nqcc.Ast.Statements;

public class Compound(Block block) : Statement
{
    public Block Block { get; } = block;
}

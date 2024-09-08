namespace Nqcc.Ast.Statements;

public class Return(Ast.Expression expression) : Statement
{
    public Ast.Expression Expression { get; } = expression;
}

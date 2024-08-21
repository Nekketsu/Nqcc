namespace Nqcc.Ast.Statements;

public class Return(Expression expression) : Statement
{
    public Expression Expression { get; } = expression;
}

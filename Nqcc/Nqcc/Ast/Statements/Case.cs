namespace Nqcc.Ast.Statements;

public class Case(Ast.Expression condition, Statement statement, string label = "") : Statement
{
    public Ast.Expression Condition { get; } = condition;
    public Statement Statement { get; } = statement;
    public string Label { get; } = label;
}

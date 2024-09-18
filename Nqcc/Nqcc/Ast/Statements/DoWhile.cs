namespace Nqcc.Ast.Statements;

public class DoWhile(Statement body, Ast.Expression condition, string label = "") : Statement
{
    public Statement Body { get; } = body;
    public Ast.Expression Condition { get; } = condition;
    public string Label { get; } = label;
}

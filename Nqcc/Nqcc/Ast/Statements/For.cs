namespace Nqcc.Ast.Statements;

public class For(ForInit init, Ast.Expression? condition, Ast.Expression? post, Statement body, string label = "") : Statement
{
    public ForInit Init { get; } = init;
    public Ast.Expression? Condition { get; } = condition;
    public Ast.Expression? Post { get; } = post;
    public Statement Body { get; } = body;
    public string Label { get; } = label;
}

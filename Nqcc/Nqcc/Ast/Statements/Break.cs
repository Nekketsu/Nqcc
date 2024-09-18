namespace Nqcc.Ast.Statements;

public class Break(string label = "") : Statement
{
    public string Label { get; } = label;
}

namespace Nqcc.Ast.Statements;

public class Label(string name, Statement statement) : Statement
{
    public string Name { get; } = name;
    public Statement Statement { get; } = statement;
}

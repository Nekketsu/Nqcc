namespace Nqcc.Ast.Statements;

public class Goto(string target) : Statement
{
    public string Target { get; } = target;
}

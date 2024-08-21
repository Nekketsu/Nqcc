namespace Nqcc.Ast.Expressions;

public class Constant(int value) : Expression
{
    public int Value { get; } = value;
}

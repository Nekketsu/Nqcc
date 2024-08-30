namespace Nqcc.Tacky.Operands;

public class Variable(string identifier) : Operand
{
    public string Identifier { get; } = identifier;
}

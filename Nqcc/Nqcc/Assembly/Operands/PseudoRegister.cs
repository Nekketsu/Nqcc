namespace Nqcc.Assembly.Operands;

public class PseudoRegister(string name) : Operand
{
    public string Name { get; } = name;
}

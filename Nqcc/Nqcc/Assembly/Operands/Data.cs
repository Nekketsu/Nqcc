namespace Nqcc.Assembly.Operands;

public class Data(string name) : Operand
{
    public string Name { get; } = name;
}

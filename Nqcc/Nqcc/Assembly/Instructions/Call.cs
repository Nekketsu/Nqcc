namespace Nqcc.Assembly.Instructions;

public class Call(string name) : Instruction
{
    public string Name { get; } = name;
}

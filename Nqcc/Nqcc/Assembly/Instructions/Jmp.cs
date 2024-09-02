namespace Nqcc.Assembly.Instructions;

public class Jmp(string target) : Instruction
{
    public string Target { get; } = target;
}

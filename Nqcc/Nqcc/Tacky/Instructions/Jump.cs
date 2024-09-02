namespace Nqcc.Tacky.Instructions;

public class Jump(string target) : Instruction
{
    public string Target { get; } = target;
}

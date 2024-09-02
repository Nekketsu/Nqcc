namespace Nqcc.Tacky.Instructions;

public class JumpIfZero(Operand condition, string target) : Instruction
{
    public Operand Condition { get; } = condition;
    public string Target { get; } = target;
}

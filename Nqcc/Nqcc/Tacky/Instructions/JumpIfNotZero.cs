namespace Nqcc.Tacky.Instructions;

public class JumpIfNotZero(Operand condition, string target) : Instruction
{
    public Operand Condition { get; } = condition;
    public string Target { get; } = target;
}

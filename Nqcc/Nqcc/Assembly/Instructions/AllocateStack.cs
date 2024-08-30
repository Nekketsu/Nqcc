namespace Nqcc.Assembly.Instructions;

public class AllocateStack(int size) : Instruction
{
    public int Size { get; } = size;
}

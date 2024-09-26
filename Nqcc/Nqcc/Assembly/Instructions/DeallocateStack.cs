namespace Nqcc.Assembly.Instructions;

public class DeallocateStack(int size) : Instruction
{
    public int Size { get; } = size;
}

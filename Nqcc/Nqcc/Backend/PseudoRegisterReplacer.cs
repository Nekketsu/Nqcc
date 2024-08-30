using Nqcc.Assembly;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using System.Collections.Immutable;

namespace Nqcc.Backend;

public class PseudoRegisterReplacer(Program tacky)
{
    private int currentOffset = 0;
    private readonly Dictionary<string, int> offsetMap = [];

    public Program Replace(out int lastStackSlot)
    {
        currentOffset = 0;
        offsetMap.Clear();

        var replacedProgram = ReplaceProgram(tacky);

        lastStackSlot = currentOffset;
        return replacedProgram;
    }

    private Program ReplaceProgram(Program program)
    {
        var function = ReplaceFunction(program.FunctionDefinition);

        return new Program(function);
    }

    private Function ReplaceFunction(Function function)
    {
        var instructions = ReplaceInstructions(function.Instructions);

        return new Function(function.Name, instructions);
    }

    private ImmutableArray<Instruction> ReplaceInstructions(ImmutableArray<Instruction> instructions)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();

        foreach (var instruction in instructions)
        {
            builder.Add(ReplaceInstruction(instruction));
        }

        return builder.ToImmutable();
    }

    private Instruction ReplaceInstruction(Instruction instruction) => instruction switch
    {
        Mov mov => ReplaceMovInstruction(mov),
        Unary unary => ReplaceUnaryInstruction(unary),
        Binary binary => ReplaceBinaryInstruction(binary),
        Idiv idiv => ReplaceIdivInstruction(idiv),
        AllocateStack => throw new Exception("Internal error: AllocateStack shouldn't be present at this point"),
        _ => instruction
    };

    private Mov ReplaceMovInstruction(Mov mov)
    {
        var source = ReplaceOperand(mov.Source);
        var destination = ReplaceOperand(mov.Destination);

        return new Mov(source, destination);
    }

    private Unary ReplaceUnaryInstruction(Unary unary)
    {
        var destination = ReplaceOperand(unary.Destination);

        return new Unary(unary.Operator, destination);
    }
    private Binary ReplaceBinaryInstruction(Binary binary)
    {
        var source = ReplaceOperand(binary.Source);
        var destination = ReplaceOperand(binary.Destination);

        return new Binary(binary.Operator, source, destination);
    }

    private Idiv ReplaceIdivInstruction(Idiv idiv)
    {
        var operand = ReplaceOperand(idiv.Operand);

        return new Idiv(operand);
    }

    private Operand ReplaceOperand(Operand operand) => operand switch
    {
        PseudoRegister pseudoRegister => ReplacePseudoRegister(pseudoRegister),
        _ => operand
    };

    private Stack ReplacePseudoRegister(PseudoRegister pseudoRegister)
    {
        if (!offsetMap.TryGetValue(pseudoRegister.Name, out var offset))
        {
            currentOffset -= 4;
            offsetMap.Add(pseudoRegister.Name, currentOffset);

            offset = currentOffset;
        }

        return new Stack(offset);
    }
}

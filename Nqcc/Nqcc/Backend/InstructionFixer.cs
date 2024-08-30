using Nqcc.Assembly;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using Nqcc.Assembly.Operands.Registers;
using System.Collections.Immutable;

namespace Nqcc.Backend;

public class InstructionFixer(Program tacky, int lastStackSlot)
{
    public Program FixUp() => FixUpProgram(tacky, lastStackSlot);

    private static Program FixUpProgram(Program program, int lastStackSlot)
    {
        var function = FixUpFunction(program.FunctionDefinition, lastStackSlot);

        return new Program(function);
    }

    private static Function FixUpFunction(Function function, int lastStackSlot)
    {
        var instructions = FixUpInstructions(function.Instructions, lastStackSlot);

        return new Function(function.Name, instructions);
    }

    private static ImmutableArray<Instruction> FixUpInstructions(ImmutableArray<Instruction> instructions, int lastStackSlot)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();

        builder.Add(new AllocateStack(-lastStackSlot));
        foreach (var instruction in instructions)
        {
            builder.AddRange(FixUpInstruction(instruction));
        }

        return builder.ToImmutable();
    }

    private static Instruction[] FixUpInstruction(Instruction instruction) => instruction switch
    {
        Mov { Source: Stack, Destination: Stack } mov => FixUpMovMemoryToMemoryInstruction(mov),
        _ => [instruction]
    };

    private static Instruction[] FixUpMovMemoryToMemoryInstruction(Mov mov) =>
    [
        new Mov(mov.Source, new R10()),
        new Mov(new R10(), mov.Destination)
    ];
}

using Nqcc.Assembly;
using Nqcc.Assembly.BinaryOperators;
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
        Idiv { Operand: Imm } idiv => FixUpIdivImmediateInstruction(idiv),
        Binary { Operator: Add or Subtract or BitwiseAnd or BitwiseOr or BitwiseXor, Source: Stack, Destination: Stack } binary => FixUpBinaryMemoryToMemoryInstrution(binary),
        Binary { Operator: LeftShift or RightShift, Source: Stack or Register } shift => FixShiftInstruction(shift),
        Binary { Operator: Multiply, Destination: Stack } multiply => FixUpMultiplyMemoryInstruction(multiply),
        _ => [instruction]
    };

    private static Instruction[] FixUpMovMemoryToMemoryInstruction(Mov mov) =>
    [
        new Mov(mov.Source, new R10()),
        new Mov(new R10(), mov.Destination)
    ];

    private static Instruction[] FixUpIdivImmediateInstruction(Idiv idiv) =>
    [
        new Mov(idiv.Operand, new R10()),
        new Idiv(new R10())
    ];

    private static Instruction[] FixUpBinaryMemoryToMemoryInstrution(Binary binary) =>
    [
        new Mov(binary.Source, new R10()),
        new Binary(binary.Operator, new R10(), binary.Destination)
    ];

    private static Instruction[] FixShiftInstruction(Binary shift) =>
    [
        new Mov(shift.Source, new CX()),
        new Binary(shift.Operator, new CX(), shift.Destination)
    ];

    private static Instruction[] FixUpMultiplyMemoryInstruction(Binary multiply) =>
    [
        new Mov(multiply.Destination, new R11()),
        new Binary(new Multiply(), multiply.Source, new R11()),
        new Mov(new R11(), multiply.Destination)
    ];
}

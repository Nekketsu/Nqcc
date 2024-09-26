using Nqcc.Assembly;
using Nqcc.Assembly.BinaryOperators;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using Nqcc.Assembly.Operands.Registers;
using Nqcc.Symbols;
using System.Collections.Immutable;

namespace Nqcc.Backend;

public class InstructionFixer(SymbolTable symbols, Program tacky)
{
    public Program FixUp() => FixUpProgram(tacky);

    private Program FixUpProgram(Program program)
    {
        var builder = ImmutableArray.CreateBuilder<FunctionDefinition>();

        foreach (var functionDefinition in program.FunctionDefinitions)
        {
            builder.Add(FixUpFunction(functionDefinition));
        }

        return new Program(builder.ToImmutable());
    }

    private FunctionDefinition FixUpFunction(FunctionDefinition functionDefinition)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();

        var function = (Function)symbols[functionDefinition.Name];
        var stackSize = (int)(Math.Ceiling(function.StackSize / 16f) * 16);

        builder.Add(new AllocateStack(stackSize));
        FixUpInstructions(builder, functionDefinition.Instructions);

        return new FunctionDefinition(functionDefinition.Name, builder.ToImmutable());
    }

    private static void FixUpInstructions(ImmutableArray<Instruction>.Builder builder, ImmutableArray<Instruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            builder.AddRange(FixUpInstruction(instruction));
        }
    }

    private static Instruction[] FixUpInstruction(Instruction instruction) => instruction switch
    {
        Mov { Source: Stack, Destination: Stack } mov => FixUpMovMemoryToMemoryInstruction(mov),
        Idiv { Operand: Imm } idiv => FixUpIdivImmediateInstruction(idiv),
        Binary { Operator: Add or Subtract or BitwiseAnd or BitwiseOr or BitwiseXor, Source: Stack, Destination: Stack } binary => FixUpBinaryMemoryToMemoryInstrution(binary),
        Binary { Operator: LeftShift or RightShift, Source: Stack or Register } shift => FixShiftInstruction(shift),
        Binary { Operator: Multiply, Destination: Stack } multiply => FixUpMultiplyMemoryInstruction(multiply),
        Cmp { Source: Stack, Destination: Stack } cmp => FixUpCmpMemoryToMemoryInstruction(cmp),
        Cmp { Destination: Imm } cmp => FixUpCmpImmediateInstruction(cmp),
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

    private static Instruction[] FixUpCmpMemoryToMemoryInstruction(Cmp cmp) =>
    [
        new Mov(cmp.Source, new R10()),
        new Cmp(new R10(), cmp.Destination)
    ];

    private static Instruction[] FixUpCmpImmediateInstruction(Cmp cmp) =>
    [
        new Mov(cmp.Destination, new R11()),
        new Cmp(cmp.Source, new R11())
    ];
}

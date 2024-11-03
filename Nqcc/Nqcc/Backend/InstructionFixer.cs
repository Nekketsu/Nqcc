using Nqcc.Assembly;
using Nqcc.Assembly.BinaryOperators;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using Nqcc.Assembly.Operands.Registers;
using System.Collections.Immutable;

namespace Nqcc.Backend;

public class InstructionFixer(Symbols.SymbolTable symbols, Program tacky)
{
    public Program FixUp() => FixUpProgram(tacky);

    private Program FixUpProgram(Program program)
    {
        var builder = ImmutableArray.CreateBuilder<TopLevel>();

        foreach (var topLevel in program.TopLevels)
        {
            builder.Add(FixUpTopLevel(topLevel));
        }

        return new Program(builder.ToImmutable());
    }

    private TopLevel FixUpTopLevel(TopLevel topLevel) => topLevel switch
    {
        Assembly.TopLevels.Function function => FixUpFunction(function),
        _ => topLevel
    };

    private Assembly.TopLevels.Function FixUpFunction(Assembly.TopLevels.Function function)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();

        var functionSymbol = symbols.GetFunction(function.Name);
        var stackSize = (int)(Math.Ceiling(functionSymbol.StackSize / 16f) * 16);

        builder.Add(new AllocateStack(stackSize));
        FixUpInstructions(builder, function.Instructions);

        return new Assembly.TopLevels.Function(function.Name, function.Global, builder.ToImmutable());
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
        Mov { Source: Stack or Data, Destination: Stack or Data } mov => FixUpMovMemoryToMemoryInstruction(mov),
        Idiv { Operand: Imm } idiv => FixUpIdivImmediateInstruction(idiv),
        Binary { Operator: Add or Subtract or BitwiseAnd or BitwiseOr or BitwiseXor, Source: Stack or Data, Destination: Stack or Data } binary => FixUpBinaryMemoryToMemoryInstrution(binary),
        Binary { Operator: LeftShift or RightShift, Source: Stack or Register or Data } shift => FixShiftInstruction(shift),
        Binary { Operator: Multiply, Destination: Stack or Data } multiply => FixUpMultiplyMemoryInstruction(multiply),
        Cmp { Source: Stack or Data, Destination: Stack or Data } cmp => FixUpCmpMemoryToMemoryInstruction(cmp),
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

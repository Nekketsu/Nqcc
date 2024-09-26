using Nqcc.Assembly;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using Nqcc.Symbols;
using System.Collections.Immutable;

namespace Nqcc.Backend;

public class PseudoRegisterReplacer(SymbolTable symbols, Program tacky)
{
    private int currentOffset = 0;
    private readonly Dictionary<string, int> offsetMap = [];

    public Program Replace()
    {
        var replacedProgram = ReplaceProgram(tacky);

        return replacedProgram;
    }

    private Program ReplaceProgram(Program program)
    {
        var builder = ImmutableArray.CreateBuilder<FunctionDefinition>();

        foreach (var functionDefinition in program.FunctionDefinitions)
        {
            builder.Add(ReplaceFunction(functionDefinition));
        }

        return new Program(builder.ToImmutable());
    }

    private FunctionDefinition ReplaceFunction(FunctionDefinition functionDefinition)
    {
        currentOffset = 0;
        offsetMap.Clear();

        var instructions = ReplaceInstructions(functionDefinition.Instructions);

        var function = (Function)symbols[functionDefinition.Name];
        symbols.AddOrReplace(new Function(function.Name, function.ParameterCount, function.IsDefined, -currentOffset));

        return new FunctionDefinition(functionDefinition.Name, instructions);
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
        Cmp cmp => ReplaceCmpInstruction(cmp),
        SetCc set => ReplaceSetCcInstruction(set),
        Push push => ReplacePushInstruction(push),
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

    private Cmp ReplaceCmpInstruction(Cmp cmp)
    {
        var source = ReplaceOperand(cmp.Source);
        var destination = ReplaceOperand(cmp.Destination);

        return new Cmp(source, destination);
    }

    private SetCc ReplaceSetCcInstruction(SetCc set)
    {
        var operand = ReplaceOperand(set.Operand);

        return new SetCc(set.ConditionCode, operand);
    }

    private Push ReplacePushInstruction(Push push)
    {
        var operand = ReplaceOperand(push.Operand);

        return new Push(operand);
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

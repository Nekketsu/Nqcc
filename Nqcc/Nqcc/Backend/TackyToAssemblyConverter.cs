using Nqcc.Assembly;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using Nqcc.Assembly.Operands.Registers;
using Nqcc.Assembly.UnaryOperators;
using System.Collections.Immutable;

namespace Nqcc.Backend;

public class TackyToAssemblyConverter(Tacky.Program tacky)
{
    public Program Convert() => ConvertProgram(tacky);

    private static Program ConvertProgram(Tacky.Program program)
    {
        var function = ConvertFunction(program.FunctionDefinition);
        return new Program(function);
    }

    private static Function ConvertFunction(Tacky.Function function)
    {
        var body = ConvertInstructions(function.Body);
        return new Function(function.Name, body);
    }

    private static ImmutableArray<Instruction> ConvertInstructions(ImmutableArray<Tacky.Instruction> instructions)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();

        foreach (var instruction in instructions)
        {
            builder.AddRange(ConvertInstrution(instruction));
        }

        return builder.ToImmutable();
    }

    private static Instruction[] ConvertInstrution(Tacky.Instruction instruction) => instruction switch
    {
        Tacky.Instructions.Return @return => ConvertReturnInstruction(@return),
        Tacky.Instructions.Unary unary => ConvertUnaryInstruction(unary),
        _ => throw new NotImplementedException()
    };

    private static Instruction[] ConvertReturnInstruction(Tacky.Instructions.Return @return)
    {
        var value = ConvertOperand(@return.Value);

        return
        [
            new Mov(value, new AX()),
            new Ret()
        ];
    }

    private static Instruction[] ConvertUnaryInstruction(Tacky.Instructions.Unary unary)
    {
        var @operator = ConvertUnaryOperator(unary.Operator);
        var source = ConvertOperand(unary.Source);
        var destination = ConvertOperand(unary.Destination);

        return
        [
            new Mov(source, destination),
            new Unary(@operator, destination)
        ];
    }

    private static UnaryOperator ConvertUnaryOperator(Tacky.UnaryOperator @operator) => @operator switch
    {
        Tacky.UnaryOperators.Complement => new Not(),
        Tacky.UnaryOperators.Negate => new Neg(),
        _ => throw new NotImplementedException()
    };

    private static Operand ConvertOperand(Tacky.Operand operand) => operand switch
    {
        Tacky.Operands.Constant constant => new Imm(constant.Value),
        Tacky.Operands.Variable variable => new PseudoRegister(variable.Identifier),
        _ => throw new NotImplementedException(),
    };
}

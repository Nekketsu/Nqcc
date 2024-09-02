using Nqcc.Assembly;
using Nqcc.Assembly.BinaryOperators;
using Nqcc.Assembly.ConditionCodes;
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
        Tacky.Instructions.Binary binary => ConvertBinaryInstruction(binary),
        Tacky.Instructions.Jump jump => ConvertJumpInstruction(jump),
        Tacky.Instructions.JumpIfZero jumpIfZero => ConvertJumpIfZeroInstruction(jumpIfZero),
        Tacky.Instructions.JumpIfNotZero jumpIfNotZero => ConvertJumpIfNotZeroInstruction(jumpIfNotZero),
        Tacky.Instructions.Copy copy => ConvertCopyInstruction(copy),
        Tacky.Instructions.Label label => ConvertLabelInstruction(label),
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
        var source = ConvertOperand(unary.Source);
        var destination = ConvertOperand(unary.Destination);

        switch (unary.Operator)
        {
            case Tacky.UnaryOperators.Not:
                return
                [
                    new Cmp(new Imm(0), source),
                    new Mov(new Imm(0), destination),
                    new SetCc(new E(), destination)
                ];
            default:
                var @operator = ConvertUnaryOperator(unary.Operator);

                return
                [
                    new Mov(source, destination),
                    new Unary(@operator, destination)
                ];
        }
    }

    private static Instruction[] ConvertBinaryInstruction(Tacky.Instructions.Binary binary)
    {
        var left = ConvertOperand(binary.Left);
        var right = ConvertOperand(binary.Right);
        var destination = ConvertOperand(binary.Destination);

        switch (binary.Operator)
        {
            case Tacky.BinaryOperators.Divide or Tacky.BinaryOperators.Modulo:
                Register resultRegister = binary.Operator is Tacky.BinaryOperators.Divide ? new AX() : new DX();
                return
                [
                    new Mov(left, new AX()),
                    new Cdq(),
                    new Idiv(right),
                    new Mov(resultRegister, destination)
                ];
            case Tacky.BinaryOperators.RelationalOperator relationOperator:
                var conditionCode = ConvertConditionCode(relationOperator);
                return
                [
                    new Cmp(right, left),
                    new Mov(new Imm(0), destination),
                    new SetCc(conditionCode, destination)
                ];
            default:
                var binaryOperator = ConvertBinaryOperator(binary.Operator);
                return
                [
                    new Mov(left, destination),
                    new Binary(binaryOperator, right, destination)
                ];
        }
    }

    private static Instruction[] ConvertJumpInstruction(Tacky.Instructions.Jump jump) =>
    [
        new Jmp(jump.Target)
    ];

    private static Instruction[] ConvertJumpIfZeroInstruction(Tacky.Instructions.JumpIfZero jumpIfZero)
    {
        var condition = ConvertOperand(jumpIfZero.Condition);

        return [
            new Cmp(new Imm(0), condition),
            new JmpCc(new E(), jumpIfZero.Target)
        ];
    }

    private static Instruction[] ConvertJumpIfNotZeroInstruction(Tacky.Instructions.JumpIfNotZero jumpIfNotZero)
    {
        var condition = ConvertOperand(jumpIfNotZero.Condition);

        return [
            new Cmp(new Imm(0), condition),
            new JmpCc(new NE(), jumpIfNotZero.Target)
        ];
    }

    private static Instruction[] ConvertCopyInstruction(Tacky.Instructions.Copy copy)
    {
        var source = ConvertOperand(copy.Source);
        var destination = ConvertOperand(copy.Destination);

        return
        [
            new Mov(source, destination)
        ];
    }

    private static Instruction[] ConvertLabelInstruction(Tacky.Instructions.Label label) =>
    [
        new Label(label.Identifier)
    ];

    private static UnaryOperator ConvertUnaryOperator(Tacky.UnaryOperator @operator) => @operator switch
    {
        Tacky.UnaryOperators.Complement => new Not(),
        Tacky.UnaryOperators.Negate => new Neg(),
        _ => throw new NotImplementedException()
    };

    private static BinaryOperator ConvertBinaryOperator(Tacky.BinaryOperator @operator) => @operator switch
    {
        Tacky.BinaryOperators.Add => new Add(),
        Tacky.BinaryOperators.Subtract => new Subtract(),
        Tacky.BinaryOperators.Multiply => new Multiply(),
        Tacky.BinaryOperators.BitwiseAnd => new BitwiseAnd(),
        Tacky.BinaryOperators.BitwiseOr => new BitwiseOr(),
        Tacky.BinaryOperators.BitwiseXor => new BitwiseXor(),
        Tacky.BinaryOperators.LeftShift => new LeftShift(),
        Tacky.BinaryOperators.RightShift => new RightShift(),
        Tacky.BinaryOperators.Divide or Tacky.BinaryOperators.Modulo => throw new Exception("Internal error: shouldn't handle division like other binary operators"),
        _ => throw new Exception()
    };

    private static Operand ConvertOperand(Tacky.Operand operand) => operand switch
    {
        Tacky.Operands.Constant constant => new Imm(constant.Value),
        Tacky.Operands.Variable variable => new PseudoRegister(variable.Identifier),
        _ => throw new NotImplementedException(),
    };

    private static ConditionCode ConvertConditionCode(Tacky.BinaryOperators.RelationalOperator relationOperator) => relationOperator switch
    {
        Tacky.BinaryOperators.RelationalOperators.Equals => new E(),
        Tacky.BinaryOperators.RelationalOperators.NotEquals => new NE(),
        Tacky.BinaryOperators.RelationalOperators.GreaterThan => new G(),
        Tacky.BinaryOperators.RelationalOperators.GreaterOrEquals => new GE(),
        Tacky.BinaryOperators.RelationalOperators.LessThan => new L(),
        Tacky.BinaryOperators.RelationalOperators.LessOrEquals => new LE(),
        _ => throw new NotImplementedException()
    };
}

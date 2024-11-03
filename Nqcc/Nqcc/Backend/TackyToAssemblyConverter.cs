using Nqcc.Assembly;
using Nqcc.Assembly.BinaryOperators;
using Nqcc.Assembly.ConditionCodes;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using Nqcc.Assembly.Operands.Registers;
using Nqcc.Assembly.TopLevels;
using Nqcc.Assembly.UnaryOperators;
using System.Collections.Immutable;

namespace Nqcc.Backend;

public class TackyToAssemblyConverter(Tacky.Program tacky, Register[]? registers = null)
{
    private readonly Register[] registers = registers ?? [new DI(), new SI(), new DX(), new CX(), new R8(), new R9()];

    public Program Convert() => ConvertProgram(tacky);

    private Program ConvertProgram(Tacky.Program program)
    {
        var builder = ImmutableArray.CreateBuilder<TopLevel>();

        foreach (var topLevel in program.TopLevels)
        {
            builder.Add(ConvertTopLevel(topLevel));
        }

        return new Program(builder.ToImmutable());
    }

    private TopLevel ConvertTopLevel(Tacky.TopLevel topLevel) => topLevel switch
    {
        Tacky.TopLevels.Function function => ConvertFunction(function),
        Tacky.TopLevels.StaticVariable staticVariable => ConvertStaticVariable(staticVariable),
        _ => throw new NotImplementedException()
    };

    private Function ConvertFunction(Tacky.TopLevels.Function function)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();

        ConvertParameters(builder, function.Parameters);
        ConvertInstructions(builder, function.Body);

        return new Function(function.Name, function.Global, builder.ToImmutable());
    }

    private StaticVariable ConvertStaticVariable(Tacky.TopLevels.StaticVariable staticVariable) => new StaticVariable(staticVariable.Name, staticVariable.Global, staticVariable.InitialValue);

    private void ConvertParameters(ImmutableArray<Instruction>.Builder builder, ImmutableArray<string> parameters)
    {
        var registerParameters = parameters.Take(registers.Length);
        var stackParameters = parameters.Skip(registers.Length);

        var registerIndex = 0;
        foreach (var registerParameter in registerParameters)
        {
            var register = registers[registerIndex];
            builder.Add(new Mov(register, new PseudoRegister(registerParameter)));

            registerIndex++;
        }

        var stackIndex = 0;
        foreach (var stackParameter in stackParameters)
        {
            var stack = new Stack(16 + (8 * stackIndex));
            builder.Add(new Mov(stack, new PseudoRegister(stackParameter)));

            stackIndex++;
        }
    }

    private void ConvertInstructions(ImmutableArray<Instruction>.Builder builder, ImmutableArray<Tacky.Instruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            builder.AddRange(ConvertInstrution(instruction));
        }
    }

    private Instruction[] ConvertInstrution(Tacky.Instruction instruction) => instruction switch
    {
        Tacky.Instructions.Return @return => ConvertReturnInstruction(@return),
        Tacky.Instructions.Unary unary => ConvertUnaryInstruction(unary),
        Tacky.Instructions.Binary binary => ConvertBinaryInstruction(binary),
        Tacky.Instructions.Jump jump => ConvertJumpInstruction(jump),
        Tacky.Instructions.JumpIfZero jumpIfZero => ConvertJumpIfZeroInstruction(jumpIfZero),
        Tacky.Instructions.JumpIfNotZero jumpIfNotZero => ConvertJumpIfNotZeroInstruction(jumpIfNotZero),
        Tacky.Instructions.Copy copy => ConvertCopyInstruction(copy),
        Tacky.Instructions.Label label => ConvertLabelInstruction(label),
        Tacky.Instructions.FunctionCall functioncall => ConvertFunctionCall(functioncall),
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

    private Instruction[] ConvertFunctionCall(Tacky.Instructions.FunctionCall functioncall)
    {
        var instructions = new List<Instruction>();

        var registerArguments = functioncall.Arguments.Take(registers.Length).ToArray();
        var stackArguments = functioncall.Arguments.Skip(registers.Length).ToArray();
        var stackPadding = stackArguments.Length % 2 == 0 ? 0 : 8;

        if (stackPadding > 0)
        {
            instructions.Add(new AllocateStack(stackPadding));
        }

        var registerIndex = 0;
        foreach (var registerArgumnet in registerArguments)
        {
            var register = registers[registerIndex];
            var assemblyArgument = ConvertOperand(registerArgumnet);
            instructions.Add(new Mov(assemblyArgument, register));

            registerIndex++;
        }

        foreach (var stackArgument in stackArguments.Reverse())
        {
            var assemblyArgument = ConvertOperand(stackArgument);
            Instruction[] argumentInstructions = assemblyArgument switch
            {
                Imm or Register => [new Push(assemblyArgument)],
                _ =>
                [
                    new Mov(assemblyArgument, new AX()),
                    new Push(new AX())
                ]
            };

            instructions.AddRange(argumentInstructions);
        }

        instructions.Add(new Call(functioncall.Name));

        var bytesToRemove = (8 * stackArguments.Length) + stackPadding;
        if (bytesToRemove > 0)
        {
            instructions.Add(new DeallocateStack(bytesToRemove));
        }

        var assemblyDestination = ConvertOperand(functioncall.Destination);
        instructions.Add(new Mov(new AX(), assemblyDestination));

        return [.. instructions];
    }

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

using Nqcc.Tacky;
using Nqcc.Tacky.BinaryOperators;
using Nqcc.Tacky.BinaryOperators.RelationalOperators;
using Nqcc.Tacky.Instructions;
using Nqcc.Tacky.Operands;
using Nqcc.Tacky.UnaryOperators;
using System.Collections.Immutable;

namespace Nqcc.Compiling;

public class TackyGenerator(Ast.Program ast)
{
    private int temporaryCounter = 0;
    private int labelCounter = 0;

    public Program Generate() => EmitProgram(ast);

    private Program EmitProgram(Ast.Program program)
    {
        var functionDefinition = EmitFunction(program.FunctionDefinition);

        return new Program(functionDefinition);
    }

    private Function EmitFunction(Ast.Function function)
    {
        var body = EmitStatement(function.Body);

        return new Function(function.Name, body);
    }

    private ImmutableArray<Instruction> EmitStatement(Ast.Statement statement) => statement switch
    {
        Ast.Statements.Return @return => EmitReturn(@return),
        _ => throw new NotImplementedException()
    };

    private ImmutableArray<Instruction> EmitReturn(Ast.Statements.Return @return)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();
        var value = EmitExpression(builder, @return.Expression);

        builder.Add(new Return(value));

        return builder.ToImmutable();
    }

    private Operand EmitExpression(ImmutableArray<Instruction>.Builder builder, Ast.Expression expression) => expression switch
    {
        Ast.Expressions.Constant constant => EmitConstant(constant),
        Ast.Expressions.Unary unary => EmitUnary(builder, unary),
        Ast.Expressions.Binary binary => EmitBinary(builder, binary),
        _ => throw new NotImplementedException()
    };

    private static Constant EmitConstant(Ast.Expressions.Constant constant)
    {
        return new Constant(constant.Value);
    }

    private Variable EmitUnary(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Unary unary)
    {
        var source = EmitExpression(builder, unary.Expression);
        var destinationName = MakeTemporary();
        var destination = new Variable(destinationName);
        var tackyOperator = ConvertUnaryOperator(unary.Operator);
        builder.Add(new Unary(tackyOperator, source, destination));

        return destination;
    }

    private Variable EmitBinary(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Binary binary) => binary.Operator switch
    {
        Ast.BinaryOperators.And => EmitBinaryAnd(builder, binary),
        Ast.BinaryOperators.Or => EmitBinaryOr(builder, binary),
        _ => EmitBinaryGeneric(builder, binary)
    };

    private Variable EmitBinaryAnd(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Binary binary)
    {
        var falseLabel = MakeLabel("and_false");
        var endLabel = MakeLabel("and_end");
        var destinationName = MakeTemporary();
        var destination = new Variable(destinationName);

        var left = EmitExpression(builder, binary.Left);
        builder.Add(new JumpIfZero(left, falseLabel));
        var right = EmitExpression(builder, binary.Right);
        builder.AddRange(
            new JumpIfZero(right, falseLabel),
            new Copy(new Constant(1), destination),
            new Jump(endLabel),
            new Label(falseLabel),
            new Copy(new Constant(0), destination),
            new Label(endLabel)
        );

        return destination;
    }

    private Variable EmitBinaryOr(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Binary binary)
    {
        var trueLabel = MakeLabel("or_true");
        var endLabel = MakeLabel("or_end");
        var destinationName = MakeTemporary();
        var destination = new Variable(destinationName);

        var left = EmitExpression(builder, binary.Left);
        builder.Add(new JumpIfNotZero(left, trueLabel));
        var right = EmitExpression(builder, binary.Right);
        builder.AddRange(
            new JumpIfNotZero(right, trueLabel),
            new Copy(new Constant(0), destination),
            new Jump(endLabel),
            new Label(trueLabel),
            new Copy(new Constant(1), destination),
            new Label(endLabel)
        );

        return destination;
    }

    private Variable EmitBinaryGeneric(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Binary binary)
    {
        var left = EmitExpression(builder, binary.Left);
        var right = EmitExpression(builder, binary.Right);
        var destinationName = MakeTemporary();
        var destination = new Variable(destinationName);
        var tackyOperator = ConvertBinaryOperator(binary.Operator);
        builder.Add(new Binary(left, tackyOperator, right, destination));

        return destination;
    }

    private static UnaryOperator ConvertUnaryOperator(Ast.UnaryOperator @operator) => @operator switch
    {
        Ast.UnaryOperators.Complement => new Complement(),
        Ast.UnaryOperators.Negate => new Negate(),
        Ast.UnaryOperators.Not => new Not(),
        _ => throw new NotImplementedException(),
    };

    private static BinaryOperator ConvertBinaryOperator(Ast.BinaryOperator @operator) => @operator switch
    {
        Ast.BinaryOperators.Add => new Add(),
        Ast.BinaryOperators.Subtract => new Subtract(),
        Ast.BinaryOperators.Multiply => new Multiply(),
        Ast.BinaryOperators.Divide => new Divide(),
        Ast.BinaryOperators.Modulo => new Modulo(),
        Ast.BinaryOperators.BitwiseAnd => new BitwiseAnd(),
        Ast.BinaryOperators.BitwiseOr => new BitwiseOr(),
        Ast.BinaryOperators.BitwiseXor => new BitwiseXor(),
        Ast.BinaryOperators.LeftShift => new LeftShift(),
        Ast.BinaryOperators.RightShift => new RightShift(),
        Ast.BinaryOperators.Equals => new Equals(),
        Ast.BinaryOperators.NotEquals => new NotEquals(),
        Ast.BinaryOperators.LessThan => new LessThan(),
        Ast.BinaryOperators.LessOrEquals => new LessOrEquals(),
        Ast.BinaryOperators.GreaterThan => new GreaterThan(),
        Ast.BinaryOperators.GreaterOrEquals => new GreaterOrEquals(),
        Ast.BinaryOperators.And or Ast.BinaryOperators.Or => throw new Exception("Internal error, cannot convert these directly to TACKY binops"),
        _ => throw new NotImplementedException()
    };

    private string MakeTemporary() => $"tmp.{temporaryCounter++}";

    private string MakeLabel(string prefix) => $"{prefix}.{labelCounter++}";
}

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
    public Program Generate() => EmitProgram(ast);

    private static Program EmitProgram(Ast.Program program)
    {
        var functionDefinition = EmitFunction(program.FunctionDefinition);

        return new Program(functionDefinition);
    }

    private static Function EmitFunction(Ast.Function function)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();

        foreach (var blockItem in function.Body)
        {
            EmitBlockItem(builder, blockItem);
        }

        builder.Add(new Return(new Constant(0)));

        return new Function(function.Name, builder.ToImmutable());
    }

    private static void EmitBlockItem(ImmutableArray<Instruction>.Builder builder, Ast.BlockItem blockItem)
    {
        switch (blockItem)
        {
            case Ast.BlockItems.Statement statement:
                EmitStatement(builder, statement.InnerStatement);
                break;
            case Ast.BlockItems.Declaration declaration:
                EmitDeclaration(builder, declaration.InnerDeclaration);
                break;
        }
    }

    private static void EmitStatement(ImmutableArray<Instruction>.Builder builder, Ast.Statement statement)
    {
        switch (statement)
        {
            case Ast.Statements.Return @return:
                EmitReturn(builder, @return);
                break;
            case Ast.Statements.Expression expression:
                EmitExpression(builder, expression.InnerExpression);
                break;
            }
    }

    private static void EmitDeclaration(ImmutableArray<Instruction>.Builder builder, Ast.Declaration declaration)
    {
        if (declaration.Initializer is not null)
        {
            EmitExpression(builder, new Ast.Expressions.Assignment(new Ast.Expressions.Variable(declaration.Name), declaration.Initializer));
        }
    }

    private static void EmitReturn(ImmutableArray<Instruction>.Builder builder, Ast.Statements.Return @return)
    {
        var value = EmitExpression(builder, @return.Expression);
        builder.Add(new Return(value));
    }

    private static Operand EmitExpression(ImmutableArray<Instruction>.Builder builder, Ast.Expression expression) => expression switch
    {
        Ast.Expressions.Constant constant => EmitConstant(constant),
        Ast.Expressions.Variable variable => EmitVariable(variable),
        Ast.Expressions.Unary unary => EmitUnary(builder, unary),
        Ast.Expressions.Binary binary => EmitBinary(builder, binary),
        Ast.Expressions.Assignment assignment => EmitAssignment(builder, assignment),
        Ast.Expressions.Compound compound => EmitCompound(builder, compound),
        Ast.Expressions.Prefix prefix => EmitPrefix(builder, prefix),
        Ast.Expressions.Postfix postfix => EmitPostfix(builder, postfix),
        _ => throw new NotImplementedException()
    };

    private static Constant EmitConstant(Ast.Expressions.Constant constant)
    {
        return new Constant(constant.Value);
    }

    private static Variable EmitVariable(Ast.Expressions.Variable variable) => new(variable.Name);

    private static Variable EmitUnary(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Unary unary)
    {
        var source = EmitExpression(builder, unary.Expression);
        var destinationName = UniqueId.MakeTemporary();
        var destination = new Variable(destinationName);
        var tackyOperator = ConvertUnaryOperator(unary.Operator);
        builder.Add(new Unary(tackyOperator, source, destination));

        return destination;
    }

    private static Variable EmitBinary(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Binary binary) => binary.Operator switch
    {
        Ast.BinaryOperators.And => EmitBinaryAnd(builder, binary),
        Ast.BinaryOperators.Or => EmitBinaryOr(builder, binary),
        _ => EmitBinaryGeneric(builder, binary)
    };

    private static Variable EmitAssignment(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Assignment assignment)
    {
        switch (assignment.Left)
        {
            case Ast.Expressions.Variable variable:
                var result = EmitExpression(builder, assignment.Right);
                var destination = new Variable(variable.Name);
                builder.Add(new Copy(result, destination));
                return destination;
            default:
                throw new Exception("Internal error: bad lvalue");
        }
    }

    private static Variable EmitCompound(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Compound compound)
    {
        switch (compound.Left)
        {
            case Ast.Expressions.Variable variable:
                var result = EmitExpression(builder, compound.Right);
                var destination = new Variable(variable.Name);
                builder.Add(new Binary(destination, ConvertCompoundOperator(compound.Operator), result, destination));
                return destination;
            default:
                throw new Exception("Internal error: bad lvalue");
        }
    }

    private static Variable EmitPrefix(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Prefix prefix)
    {
        switch (prefix.Expression)
        {
            case Ast.Expressions.Variable variable:
                var destination = new Variable(variable.Name);
                builder.Add(new Binary(destination, ConvertPrefixOperator(prefix.Operator), new Constant(1), destination));
                return destination;
            default:
                throw new Exception("Internal error: bad lvalue");
        }
    }

    private static Variable EmitPostfix(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Postfix postfix)
    {
        switch (postfix.Expression)
        {
            case Ast.Expressions.Variable variable:
                var destination = new Variable(variable.Name);
                var copy = new Variable(UniqueId.MakeTemporary());
                builder.Add(new Copy(destination, copy));
                builder.Add(new Binary(destination, ConvertPostfixOperator(postfix.Operator), new Constant(1), destination));
                return copy;
            default:
                throw new Exception("Internal error: bad lvalue");
        }
    }

    private static Variable EmitBinaryAnd(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Binary binary)
    {
        var falseLabel = UniqueId.MakeLabel("and_false");
        var endLabel = UniqueId.MakeLabel("and_end");
        var destinationName = UniqueId.MakeTemporary();
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

    private static Variable EmitBinaryOr(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Binary binary)
    {
        var trueLabel = UniqueId.MakeLabel("or_true");
        var endLabel = UniqueId.MakeLabel("or_end");
        var destinationName = UniqueId.MakeTemporary();
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

    private static Variable EmitBinaryGeneric(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Binary binary)
    {
        var left = EmitExpression(builder, binary.Left);
        var right = EmitExpression(builder, binary.Right);
        var destinationName = UniqueId.MakeTemporary();
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

    private static BinaryOperator ConvertCompoundOperator(Ast.CompoundOperator @operator) => @operator switch
    {
        Ast.CompoundOperators.AddAssignment => new Add(),
        Ast.CompoundOperators.SubtractAssignment => new Subtract(),
        Ast.CompoundOperators.MultiplyAssignment => new Multiply(),
        Ast.CompoundOperators.DivideAssignment => new Divide(),
        Ast.CompoundOperators.ModuloAssignment => new Modulo(),
        Ast.CompoundOperators.BitwiseAndAssignment => new BitwiseAnd(),
        Ast.CompoundOperators.BitwiseOrAssignment => new BitwiseOr(),
        Ast.CompoundOperators.BitwiseXorAssignment => new BitwiseXor(),
        Ast.CompoundOperators.LeftShiftAssignment => new LeftShift(),
        Ast.CompoundOperators.RightShiftAssignment => new RightShift(),
        _ => throw new NotImplementedException()
    };


    private static BinaryOperator ConvertPrefixOperator(Ast.PrefixOperator prefixOperator) => prefixOperator switch
    {
        Ast.PrefixOperators.Increment => new Add(),
        Ast.PrefixOperators.Decrement => new Subtract(),
        _ => throw new NotImplementedException()
    };

    private static BinaryOperator ConvertPostfixOperator(Ast.PostfixOperator postfixOperator) => postfixOperator switch
    {
        Ast.PostfixOperators.Increment => new Add(),
        Ast.PostfixOperators.Decrement => new Subtract(),
        _ => throw new NotImplementedException()
    };
}

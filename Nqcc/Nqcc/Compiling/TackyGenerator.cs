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
        var builder = ImmutableArray.CreateBuilder<FunctionDefinition>();

        foreach (var functionDeclaration in program.FunctionDeclarations)
        {
            var functionDefinition = EmitFunctionDeclaration(functionDeclaration);
            if (functionDefinition is not null)
            {
                builder.Add(functionDefinition);
            }
        }

        return new Program(builder.ToImmutable());
    }

    private static FunctionDefinition? EmitFunctionDeclaration(Ast.Declarations.FunctionDeclaration functionDeclaration)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();

        if (functionDeclaration.Body is not null)
        {
            EmitBlock(builder, functionDeclaration.Body);
            builder.Add(new Return(new Constant(0)));

            return new FunctionDefinition(functionDeclaration.Name, functionDeclaration.Parameters, builder.ToImmutable());
        }

        return null;
    }

    private static void EmitBlock(ImmutableArray<Instruction>.Builder builder, Ast.Block block)
    {
        foreach (var blockItem in block.BlockItems)
        {
            EmitBlockItem(builder, blockItem);
        }
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
            case Ast.Statements.If @if:
                EmitIf(builder, @if);
                break;
            case Ast.Statements.Label label:
                EmitLabel(builder, label);
                break;
            case Ast.Statements.Goto @goto:
                EmitGoto(builder, @goto);
                break;
            case Ast.Statements.Compound compound:
                EmitCompound(builder, compound);
                break;
            case Ast.Statements.Break @break:
                EmitBreak(builder, @break);
                break;
            case Ast.Statements.Continue @continue:
                EmitContinue(builder, @continue);
                break;
            case Ast.Statements.DoWhile doWhile:
                EmitDoWhile(builder, doWhile);
                break;
            case Ast.Statements.While @while:
                EmitWhile(builder, @while);
                break;
            case Ast.Statements.For @for:
                EmitFor(builder, @for);
                break;
            case Ast.Statements.Switch @switch:
                EmitSwitch(builder, @switch);
                break;
            case Ast.Statements.Case @case:
                EmitCase(builder, @case);
                break;
            case Ast.Statements.Default @default:
                EmitDefault(builder, @default);
                break;
        }
    }

    private static void EmitDeclaration(ImmutableArray<Instruction>.Builder builder, Ast.Declaration declaration)
    {
        if (declaration is Ast.Declarations.VariableDeclaration variableDeclaration && variableDeclaration.Initializer is not null)
        {
            EmitExpression(builder, new Ast.Expressions.Assignment(new Ast.Expressions.Variable(variableDeclaration.Name), variableDeclaration.Initializer));
        }
    }

    private static void EmitReturn(ImmutableArray<Instruction>.Builder builder, Ast.Statements.Return @return)
    {
        var value = EmitExpression(builder, @return.Expression);
        builder.Add(new Return(value));
    }

    private static void EmitIf(ImmutableArray<Instruction>.Builder builder, Ast.Statements.If @if)
    {
        if (@if.Else is null)
        {
            var endLabel = UniqueId.MakeLabel("if_end");

            var condition = EmitExpression(builder, @if.Condition);
            builder.Add(new JumpIfZero(condition, endLabel));
            EmitStatement(builder, @if.Then);
            builder.Add(new Label(endLabel));
        }
        else
        {
            var elseLabel = UniqueId.MakeLabel("else");
            var endLabel = UniqueId.MakeLabel("if_end");

            var condition = EmitExpression(builder, @if.Condition);
            builder.Add(new JumpIfZero(condition, elseLabel));
            EmitStatement(builder, @if.Then);
            builder.Add(new Jump(endLabel));
            builder.Add(new Label(elseLabel));
            EmitStatement(builder, @if.Else);
            builder.Add(new Label(endLabel));
        }
    }

    private static void EmitLabel(ImmutableArray<Instruction>.Builder builder, Ast.Statements.Label label)
    {
        builder.Add(new Label(label.Name));
        EmitStatement(builder, label.Statement);
    }

    private static void EmitGoto(ImmutableArray<Instruction>.Builder builder, Ast.Statements.Goto @goto)
    {
        builder.Add(new Jump(@goto.Target));
    }

    private static void EmitCompound(ImmutableArray<Instruction>.Builder builder, Ast.Statements.Compound compound)
    {
        EmitBlock(builder, compound.Block);
    }

    private static void EmitBreak(ImmutableArray<Instruction>.Builder builder, Ast.Statements.Break @break)
    {
        builder.Add(new Jump(BreakLabel(@break.Label)));
    }

    private static void EmitContinue(ImmutableArray<Instruction>.Builder builder, Ast.Statements.Continue @continue)
    {
        builder.Add(new Jump(ContinueLabel(@continue.Label)));
    }

    private static void EmitDoWhile(ImmutableArray<Instruction>.Builder builder, Ast.Statements.DoWhile doWhile)
    {
        var startLabel = StartLabel(doWhile.Label);
        var continueLabel = ContinueLabel(doWhile.Label);
        var breakLabel = BreakLabel(doWhile.Label);

        builder.Add(new Label(startLabel));
        EmitStatement(builder, doWhile.Body);
        builder.Add(new Label(continueLabel));
        var condition = EmitExpression(builder, doWhile.Condition);
        builder.Add(new JumpIfNotZero(condition, startLabel));
        builder.Add(new Label(breakLabel));
    }

    private static void EmitWhile(ImmutableArray<Instruction>.Builder builder, Ast.Statements.While @while)
    {
        var continueLabel = ContinueLabel(@while.Label);
        var breakLabel = BreakLabel(@while.Label);

        builder.Add(new Label(continueLabel));
        var condition = EmitExpression(builder, @while.Condition);
        builder.Add(new JumpIfZero(condition, breakLabel));
        EmitStatement(builder, @while.Body);
        builder.Add(new Jump(continueLabel));
        builder.Add(new Label(breakLabel));
    }

    private static void EmitFor(ImmutableArray<Instruction>.Builder builder, Ast.Statements.For @for)
    {
        var startLabel = StartLabel(@for.Label);
        var continueLabel = ContinueLabel(@for.Label);
        var breakLabel = BreakLabel(@for.Label);

        EmitForInit(builder, @for.Init);
        builder.Add(new Label(startLabel));
        if (@for.Condition is not null)
        {
            var condition = EmitExpression(builder, @for.Condition);
            builder.Add(new JumpIfZero(condition, breakLabel));
        }
        EmitStatement(builder, @for.Body);
        builder.Add(new Label(continueLabel));
        if (@for.Post is not null)
        {
            EmitExpression(builder, @for.Post);
        }
        builder.Add(new Jump(startLabel));
        builder.Add(new Label(breakLabel));
    }

    private static void EmitSwitch(ImmutableArray<Instruction>.Builder builder, Ast.Statements.Switch @switch)
    {
        var breakLabel = BreakLabel(@switch.Label);
        var destinationName = UniqueId.MakeTemporary();

        var destination = new Variable(destinationName);
        var left = EmitExpression(builder, @switch.Condition);

        foreach (var @case in @switch.Cases)
        {
            var right = EmitExpression(builder, @case.Condition);
            var compare = new Binary(left, new Equals(), right, destination);
            builder.Add(compare);
            builder.Add(new JumpIfNotZero(destination, @case.Label));
        }

        if (@switch.Default is not null)
        {
            builder.Add(new Jump(@switch.Default.Label));
        }

        builder.Add(new Jump(breakLabel));

        EmitStatement(builder, @switch.Body);
        builder.Add(new Label(breakLabel));
    }

    private static void EmitCase(ImmutableArray<Instruction>.Builder builder, Ast.Statements.Case @case)
    {
        builder.Add(new Label(@case.Label));
        EmitStatement(builder, @case.Statement);
    }

    private static void EmitDefault(ImmutableArray<Instruction>.Builder builder, Ast.Statements.Default @default)
    {
        builder.Add(new Label(@default.Label));
        EmitStatement(builder, @default.Statement);
    }

    private static void EmitForInit(ImmutableArray<Instruction>.Builder builder, Ast.ForInit init)
    {
        switch (init)
        {
            case Ast.ForInits.InitDeclaration { Declaration: Ast.Declaration declaration }:
                EmitDeclaration(builder, declaration);
                break;
            case Ast.ForInits.InitExpression { Expression: Ast.Expression expression }:
                EmitExpression(builder, expression);
                break;
        }
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
        Ast.Expressions.Conditional conditional => EmitConditional(builder, conditional),
        Ast.Expressions.FunctionCall functionCall => EmitFunctionCall(builder, functionCall),
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

    private static Variable EmitConditional(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Conditional conditional)
    {
        var elseLabel = UniqueId.MakeLabel("conditional_else");
        var endLabel = UniqueId.MakeLabel("end");
        var destinationName = UniqueId.MakeTemporary();
        var destination = new Variable(destinationName);

        var condition = EmitExpression(builder, conditional.Condition);
        builder.Add(new JumpIfZero(condition, elseLabel));
        var thenResult = EmitExpression(builder, conditional.Then);
        builder.Add(new Copy(thenResult, destination));
        builder.Add(new Jump(endLabel));
        builder.Add(new Label(elseLabel));
        var elseResult = EmitExpression(builder, conditional.Else);
        builder.Add(new Copy(elseResult, destination));
        builder.Add(new Label(endLabel));

        return destination;
    }

    private static Variable EmitFunctionCall(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.FunctionCall functionCall)
    {
        var destinationName = UniqueId.MakeTemporary();
        var destination = new Variable(destinationName);

        var argumentBuilder = ImmutableArray.CreateBuilder<Operand>();

        foreach (var argument in functionCall.Arguments)
        {
            var argumentResult = EmitExpression(builder, argument);
            argumentBuilder.Add(argumentResult);
        }

        builder.Add(new FunctionCall(functionCall.Name, argumentBuilder.ToImmutable(), destination));

        return destination;
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

    private static string StartLabel(string label) => $"start.{label}";
    private static string BreakLabel(string label) => $"break.{label}";
    private static string ContinueLabel(string label) => $"continue.{label}";
}

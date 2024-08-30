using Nqcc.Assembly;
using Nqcc.Assembly.BinaryOperators;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using Nqcc.Assembly.Operands.Registers;
using Nqcc.Assembly.UnaryOperators;

namespace Nqcc.Compiling;

public abstract class CodeEmitter(TextWriter writer) : ICodeEmitter
{
    protected TextWriter writer = writer;

    public void Emit(Program assembly) => EmitProgram(assembly);

    protected abstract string GetName(string name);

    protected abstract void EmitStackNote();

    private void EmitProgram(Program program)
    {
        EmitFunction(program.FunctionDefinition);
        EmitStackNote();
    }

    private void EmitFunction(Function function)
    {
        var name = GetName(function.Name);
        writer.WriteLine($"\t.globl {name}");
        writer.WriteLine($"{name}:");
        writer.WriteLine("\tpushq\t%rbp");
        writer.WriteLine("\tmovq\t%rsp, %rbp");

        foreach (var instruction in function.Instructions)
        {
            EmitInstruction(instruction);
        }
    }

    private void EmitInstruction(Instruction instruction)
    {
        switch (instruction)
        {
            case Mov mov:
                EmitMovInstruction(mov);
                break;
            case Ret:
                EmitRetInstruction();
                break;
            case Unary unary:
                EmitUnaryInstruction(unary);
                break;
            case Binary binary:
                EmitBinaryInstruction(binary);
                break;
            case Idiv idiv:
                EmitIdivInstruction(idiv);
                break;
            case Cdq:
                EmitCdqInstruction();
                break;
            case AllocateStack allocateStack:
                EmitAllocateStackInstruction(allocateStack);
                break;
        }
    }

    private void EmitMovInstruction(Mov mov)
    {
        writer.WriteLine($"\tmovl\t{ShowOperand(mov.Source)}, {ShowOperand(mov.Destination)}");
    }

    private void EmitRetInstruction()
    {
        writer.WriteLine("\tmovq\t%rbp, %rsp");
        writer.WriteLine("\tpopq\t%rbp");
        writer.WriteLine($"\tret");
    }

    private void EmitUnaryInstruction(Unary unary)
    {
        writer.WriteLine($"\t{ShowUnaryOperator(unary.Operator)}\t{ShowOperand(unary.Destination)}");
    }

    private void EmitBinaryInstruction(Binary binary)
    {
        switch (binary.Operator)
        {
            case LeftShift or RightShift:
                writer.WriteLine($"\t{ShowBinaryOperator(binary.Operator)}\t{ShowByteOperand(binary.Source)}, {ShowOperand(binary.Destination)}");
                break;
            default:
                writer.WriteLine($"\t{ShowBinaryOperator(binary.Operator)}\t{ShowOperand(binary.Source)}, {ShowOperand(binary.Destination)}");
                break;
        }
    }

    private void EmitIdivInstruction(Idiv idiv)
    {
        writer.WriteLine($"\tidivl\t{ShowOperand(idiv.Operand)}");
    }

    private void EmitCdqInstruction()
    {
        writer.WriteLine("\tcdq");
    }

    private static string ShowUnaryOperator(UnaryOperator @operator) => @operator switch
    {
        Neg => "negl",
        Not => "notl",
        _ => throw new NotImplementedException()
    };

    private static string ShowBinaryOperator(BinaryOperator @operator) => @operator switch
    {
        Add => "addl",
        Subtract => "subl",
        Multiply => "imull",
        BitwiseAnd => "andl",
        BitwiseOr => "orl",
        BitwiseXor => "xorl",
        LeftShift => "sall",
        RightShift => "sarl",
        _ => throw new NotImplementedException()
    };

    private void EmitAllocateStackInstruction(AllocateStack allocateStack)
    {
        writer.WriteLine($"\tsubq\t${allocateStack.Size}, %rsp");
    }

    private static string ShowOperand(Operand operand) => operand switch
    {
        Register register => ShowRegisterOperand(register),
        Stack stack => ShowStackOperand(stack),
        Imm imm => ShowImmOperand(imm),
        _ => throw new NotImplementedException()
    };
    
    private static string ShowByteOperand(Operand operand) => operand switch
    {
        AX => "%al",
        CX => "%cl",
        DX => "%dl",
        R10 => "%r10b",
        R11 => "%r11b",
        _ => ShowOperand(operand)
    };

    private static string ShowRegisterOperand(Register register) => register switch
    {
        AX => "%eax",
        CX => "%ecx",
        DX => "%edx",
        R10 => "%r10d",
        R11 => "%r11d",
        _ => throw new NotImplementedException()
    };

    private static string ShowStackOperand(Stack stack) => $"{stack.Offset}(%rbp)";

    private static string ShowImmOperand(Imm imm) => $"${imm.Value}";
}

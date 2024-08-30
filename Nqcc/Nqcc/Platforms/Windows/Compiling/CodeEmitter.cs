using Nqcc.Assembly;
using Nqcc.Assembly.BinaryOperators;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using Nqcc.Assembly.Operands.Registers;
using Nqcc.Assembly.UnaryOperators;
using Nqcc.Compiling;

namespace Nqcc.Platforms.Windows.Compiling;

public class CodeEmitter(TextWriter writer) : ICodeEmitter
{
    public void Emit(Program assembly) => EmitProgram(assembly);

    private void EmitProgram(Program program)
    {
        writer.WriteLine(".code");
        writer.WriteLine();
        EmitFunction(program.FunctionDefinition);
        writer.WriteLine();
        writer.WriteLine("END");
    }

    private void EmitFunction(Function function)
    {
        writer.WriteLine($"PUBLIC\t{function.Name}");
        writer.WriteLine($"{function.Name}\tPROC");
        writer.WriteLine("\tpush\trbp");
        writer.WriteLine("\tmov\trbp, rsp");

        foreach (var instruction in function.Instructions)
        {
            EmitInstruction(instruction);
        }

        writer.WriteLine($"{function.Name}\tENDP");
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
        writer.WriteLine($"\tmov\t{ShowOperand(mov.Destination)}, {ShowOperand(mov.Source)}");
    }

    private void EmitRetInstruction()
    {
        writer.WriteLine("\tmov\trsp, rbp");
        writer.WriteLine("\tpop\trbp");
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
                writer.WriteLine($"\t{ShowBinaryOperator(binary.Operator)}\t{ShowOperand(binary.Destination)}, {ShowByteOperand(binary.Source)}");
                break;
            default:
                writer.WriteLine($"\t{ShowBinaryOperator(binary.Operator)}\t{ShowOperand(binary.Destination)}, {ShowOperand(binary.Source)}");
                break;
        }
    }

    private void EmitIdivInstruction(Idiv idiv)
    {
        writer.WriteLine($"\tidiv\t{ShowOperand(idiv.Operand)}");
    }

    private void EmitCdqInstruction()
    {
        writer.WriteLine("\tcdq");
    }

    private static string ShowUnaryOperator(UnaryOperator @operator) => @operator switch
    {
        Neg => "neg",
        Not => "not",
        _ => throw new NotImplementedException()
    };

    private static string ShowBinaryOperator(BinaryOperator @operator) => @operator switch
    {
        Add => "add",
        Subtract => "sub",
        Multiply => "imul",
        BitwiseAnd => "and",
        BitwiseOr => "or",
        BitwiseXor => "xor",
        LeftShift => "sal",
        RightShift => "sar",
        _ => throw new NotImplementedException()
    };

    private void EmitAllocateStackInstruction(AllocateStack allocateStack)
    {
        writer.WriteLine($"\tsub\trsp, {allocateStack.Size}");
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
        Register register => ShowByteRegisterOperand(register),
        Stack stack => ShowByteStackOperand(stack),
        Imm imm => ShowImmOperand(imm),
        _ => throw new NotImplementedException()
    };

    private static string ShowRegisterOperand(Register register) => register switch
    {
        AX => "eax",
        CX => "ecx",
        DX => "edx",
        R10 => "r10d",
        R11 => "r11d",
        _ => throw new NotImplementedException()
    };

    private static string ShowStackOperand(Stack stack) => stack.Offset switch
    {
        < 0 => $"DWORD PTR [rbp - {-stack.Offset}]",
        0 => "DWORD PTR [rbp]",
        > 0 => $"DWORD PTR [rbp + {stack.Offset}]"
    };

    private static string ShowImmOperand(Imm imm) => $"{imm.Value}";

    private static string ShowByteRegisterOperand(Register register) => register switch
    {
        AX => "al",
        CX => "cl",
        DX => "dl",
        R10 => "r10b",
        R11 => "r11b",
        _ => throw new NotImplementedException()
    };

    private static string ShowByteStackOperand(Stack stack) => stack.Offset switch
    {
        < 0 => $"BYTE PTR [rbp - {-stack.Offset}]",
        0 => "BYTE PTR [rbp]",
        > 0 => $"BYTE PTR [rbp + {stack.Offset}]"
    };
}

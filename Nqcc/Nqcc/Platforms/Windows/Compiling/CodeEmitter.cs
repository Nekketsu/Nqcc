using Nqcc.Assembly;
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

    private static string ShowUnaryOperator(UnaryOperator @operator) => @operator switch
    {
        Neg => "neg",
        Not => "not",
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

    private static string ShowRegisterOperand(Register register) => register switch
    {
        AX => "eax",
        R10 => "r10d",
        _ => throw new NotImplementedException()
    };

    private static string ShowStackOperand(Stack stack) => stack.Offset switch
    {
        < 0 => $"DWORD PTR [rbp - {-stack.Offset}]",
        0 => "DWORD PTR [rbp]",
        > 0 => $"DWORD PTR [rbp + {stack.Offset}]"
    };

    private static string ShowImmOperand(Imm imm) => $"{imm.Value}";
}

using Nqcc.Assembly;
using Nqcc.Assembly.BinaryOperators;
using Nqcc.Assembly.ConditionCodes;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using Nqcc.Assembly.Operands.Registers;
using Nqcc.Assembly.TopLevels;
using Nqcc.Assembly.UnaryOperators;
using Nqcc.Compiling;

namespace Nqcc.Platforms.Windows.Compiling;

public class CodeEmitter(TextWriter writer) : ICodeEmitter
{
    public void Emit(Program assembly) => EmitProgram(assembly);

    private static string GetLabelName(string name)
    {
        return $"{name.Replace('.', '_')}";
    }

    private void EmitProgram(Program program)
    {
        foreach (var topLevel in program.TopLevels)
        {
            EmitTopLevel(topLevel);
            writer.WriteLine();
        }

        writer.WriteLine();
        writer.WriteLine("END");
    }

    private void EmitTopLevel(TopLevel topLevel)
    {
        switch (topLevel)
        {
            case Function function:
                EmitFunction(function);
                break;
            case StaticVariable staticVariable:
                EmitStaticVariable(staticVariable);
                break;
        }
    }

    private void EmitFunction(Function functionDefinition)
    {
        writer.WriteLine("\t.code");
        writer.WriteLine($"PUBLIC\t{functionDefinition.Name}");
        writer.WriteLine($"{functionDefinition.Name}\tPROC");
        writer.WriteLine("\tpush\trbp");
        writer.WriteLine("\tmov\trbp, rsp");

        foreach (var instruction in functionDefinition.Instructions)
        {
            EmitInstruction(instruction);
        }

        writer.WriteLine($"{functionDefinition.Name}\tENDP");
    }
    private void EmitStaticVariable(StaticVariable staticVariable)
    {
        if (staticVariable.InitialValue == 0)
        {
            EmitZeroInitializedStaticVariable(staticVariable);
        }
        else
        {
            EmitNonZeroInitializedStaticVariable(staticVariable);
        }
    }

    private void EmitZeroInitializedStaticVariable(StaticVariable staticVariable)
    {
        var name = GetLabelName(staticVariable.Name);
        writer.WriteLine("\t.data?");
        writer.WriteLine("\talign\t4");
        writer.WriteLine($"{name}\tdword");
    }

    private void EmitNonZeroInitializedStaticVariable(StaticVariable staticVariable)
    {
        var name = GetLabelName(staticVariable.Name);
        writer.WriteLine("\t.data");
        writer.WriteLine("\talign\t4");
        writer.WriteLine($"{name}\tdword\t{staticVariable.InitialValue}");
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
            case Cmp cmp:
                EmitCmpInstruction(cmp);
                break;
            case Jmp jmp:
                EmitJmpInstruction(jmp);
                break;
            case JmpCc jmp:
                EmitJmpCcInstruction(jmp);
                break;
            case SetCc set:
                EmitSetCcInstruction(set);
                break;
            case Label label:
                EmitLabelInstruction(label);
                break;
            case DeallocateStack deallocateStack:
                EmitDeallocateStack(deallocateStack);
                break;
            case Push push:
                EmitPush(push);
                break;
            case Call call:
                EmitCall(call);
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

    private void EmitCmpInstruction(Cmp cmp)
    {
        writer.WriteLine($"\tcmp\t{ShowOperand(cmp.Destination)}, {ShowOperand(cmp.Source)}");
    }

    private void EmitJmpInstruction(Jmp jmp)
    {
        writer.WriteLine($"\tjmp\t{GetLabelName(jmp.Target)}");
    }

    private void EmitJmpCcInstruction(JmpCc jmp)
    {
        writer.WriteLine($"\tj{ShowConditionCode(jmp.ConditionCode)}\t{GetLabelName(jmp.Target)}");
    }

    private void EmitSetCcInstruction(SetCc set)
    {
        writer.WriteLine($"\tset{ShowConditionCode(set.ConditionCode)}\t{ShowByteOperand(set.Operand)}");
    }

    private void EmitLabelInstruction(Label label)
    {
        writer.WriteLine($"{GetLabelName(label.Identifier)}:");
    }

    private void EmitDeallocateStack(DeallocateStack deallocateStack)
    {
        writer.WriteLine($"\tadd\trsp, {deallocateStack.Size}");
    }

    private void EmitPush(Push push)
    {
        writer.WriteLine($"\tpush\t{ShowQuadwordOperand(push.Operand)}");
    }

    private void EmitCall(Call call)
    {
        writer.WriteLine($"\tcall\t{call.Name}");
    }

    private static string ShowOperand(Operand operand) => operand switch
    {
        Register register => ShowRegisterOperand(register),
        Stack stack => ShowStackOperand(stack),
        Imm imm => ShowImmOperand(imm),
        Data data => ShowDataOperand(data),
        _ => throw new NotImplementedException()
    };

    private static string ShowByteOperand(Operand operand) => operand switch
    {
        Register register => ShowByteRegisterOperand(register),
        Stack stack => ShowByteStackOperand(stack),
        Imm imm => ShowImmOperand(imm),
        _ => throw new NotImplementedException()
    };

    private static string ShowQuadwordOperand(Operand operand) => operand switch
    {
        Register register => ShowQuadwordRegisterOperand(register),
        _ => ShowOperand(operand)
    };

    private static string ShowRegisterOperand(Register register) => register switch
    {
        AX => "eax",
        CX => "ecx",
        DX => "edx",
        DI => "edi",
        SI => "esi",
        R8 => "r8d",
        R9 => "r9d",
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

    private static string ShowDataOperand(Data data) => GetLabelName(data.Name);

    private static string ShowByteRegisterOperand(Register register) => register switch
    {
        AX => "al",
        CX => "cl",
        DX => "dl",
        DI => "dil",
        SI => "sil",
        R8 => "r8b",
        R9 => "r9b",
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

    private static string ShowQuadwordRegisterOperand(Register register) => register switch
    {
        AX => "rax",
        CX => "rcx",
        DX => "rdx",
        DI => "rdi",
        SI => "rsi",
        R8 => "r8",
        R9 => "r9",
        R10 => "r10",
        R11 => "r11",
        _ => throw new NotImplementedException()
    };

    private static string ShowConditionCode(ConditionCode conditionCode) => conditionCode switch
    {
        E => "e",
        NE => "ne",
        L => "l",
        LE => "le",
        G => "g",
        GE => "ge",
        _ => throw new NotImplementedException()
    };
}

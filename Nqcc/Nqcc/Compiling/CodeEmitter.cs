using Nqcc.Assembly;
using Nqcc.Assembly.BinaryOperators;
using Nqcc.Assembly.ConditionCodes;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using Nqcc.Assembly.Operands.Registers;
using Nqcc.Assembly.TopLevels;
using Nqcc.Assembly.UnaryOperators;

namespace Nqcc.Compiling;

public abstract class CodeEmitter(TextWriter writer) : ICodeEmitter
{
    protected TextWriter writer = writer;

    public void Emit(Program assembly) => EmitProgram(assembly);

    protected abstract string GetLabelName(string name);

    protected abstract string GetLocalLabelName(string name);

    protected abstract string GetFunctionName(string name);

    protected abstract string GetAlignDirective(int alignment);

    protected abstract void EmitStackNote();

    private void EmitProgram(Program program)
    {
        foreach (var topLevel in program.TopLevels)
        {
            EmitTopLevel(topLevel);
            writer.WriteLine();
        }

        EmitStackNote();
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

    private void EmitFunction(Function function)
    {
        var name = GetLabelName(function.Name);
        if (function.Global)
        {
            writer.WriteLine($"\t.globl {name}");
        }
        writer.WriteLine("\t.text");
        writer.WriteLine($"{name}:");
        writer.WriteLine("\tpushq\t%rbp");
        writer.WriteLine("\tmovq\t%rsp, %rbp");

        foreach (var instruction in function.Instructions)
        {
            EmitInstruction(instruction);
        }
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
        if (staticVariable.Global)
        {
            writer.WriteLine($"\t.globl {name}");
        }
        writer.WriteLine("\t.bss");
        writer.WriteLine($"\t{GetAlignDirective(4)}");
        writer.WriteLine($"{name}:");
        writer.WriteLine("\t.zero 4");
    }

    private void EmitNonZeroInitializedStaticVariable(StaticVariable staticVariable)
    {
        var name = GetLabelName(staticVariable.Name);
        if (staticVariable.Global)
        {
            writer.WriteLine($"\t.globl {name}");
        }
        writer.WriteLine("\t.data");
        writer.WriteLine($"\t{GetAlignDirective(4)}");
        writer.WriteLine($"{name}:");
        writer.WriteLine($"\t.long {staticVariable.InitialValue}");
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

    private void EmitCmpInstruction(Cmp cmp)
    {
        writer.WriteLine($"\tcmpl\t{ShowOperand(cmp.Source)}, {ShowOperand(cmp.Destination)}");
    }

    private void EmitJmpInstruction(Jmp jmp)
    {
        writer.WriteLine($"\tjmp\t{GetLocalLabelName(jmp.Target)}");
    }

    private void EmitJmpCcInstruction(JmpCc jmp)
    {
        writer.WriteLine($"\tj{ShowConditionCode(jmp.ConditionCode)}\t{GetLocalLabelName(jmp.Target)}");
    }

    private void EmitSetCcInstruction(SetCc set)
    {
        writer.WriteLine($"\tset{ShowConditionCode(set.ConditionCode)}\t{ShowByteOperand(set.Operand)}");
    }

    private void EmitLabelInstruction(Label label)
    {
        writer.WriteLine($"{GetLocalLabelName(label.Identifier)}:");
    }

    private void EmitDeallocateStack(DeallocateStack deallocateStack)
    {
        writer.WriteLine($"\taddq\t${deallocateStack.Size}, %rsp");
    }

    private void EmitPush(Push push)
    {
        writer.WriteLine($"\tpushq\t{ShowQuadwordOperand(push.Operand)}");
    }

    private void EmitCall(Call call)
    {
        writer.WriteLine($"\tcall\t{GetFunctionName(call.Name)}");
    }

    private static string ShowOperand(Operand operand) => operand switch
    {
        Register register => ShowRegisterOperand(register),
        Stack stack => ShowStackOperand(stack),
        Imm imm => ShowImmOperand(imm),
        Data data => ShowDataOperand(data),
        _ => throw new NotImplementedException()
    };

    private static string ShowQuadwordOperand(Operand operand) => operand switch
    {
        Register register => ShowQuadwordRegisterOperand(register),
        _ => ShowOperand(operand)
    };

    private static string ShowRegisterOperand(Register register) => register switch
    {
        AX => "%eax",
        CX => "%ecx",
        DX => "%edx",
        DI => "%edi",
        SI => "%esi",
        R8 => "%r8d",
        R9 => "%r9d",
        R10 => "%r10d",
        R11 => "%r11d",
        _ => throw new NotImplementedException()
    };

    private static string ShowStackOperand(Stack stack) => $"{stack.Offset}(%rbp)";

    private static string ShowImmOperand(Imm imm) => $"${imm.Value}";

    private static string ShowDataOperand(Data data) => $"{data.Name}(%rip)";

    private static string ShowByteOperand(Operand operand) => operand switch
    {
        AX => "%al",
        CX => "%cl",
        DX => "%dl",
        DI => "%dil",
        SI => "%sil",
        R8 => "%r8b",
        R9 => "%r9b",
        R10 => "%r10b",
        R11 => "%r11b",
        _ => ShowOperand(operand)
    };

    private static string ShowQuadwordRegisterOperand(Register register) => register switch
    {
        AX => "%rax",
        CX => "%rcx",
        DX => "%rdx",
        DI => "%rdi",
        SI => "%rsi",
        R8 => "%r8",
        R9 => "%r9",
        R10 => "%r10",
        R11 => "%r11",
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

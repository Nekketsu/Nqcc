using Nqcc.Assembly;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;

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
        }
    }

    private void EmitMovInstruction(Mov mov)
    {
        writer.WriteLine($"\tmovl\t{ShowOperand(mov.Source)}, {ShowOperand(mov.Destination)}");
    }

    private void EmitRetInstruction()
    {
        writer.WriteLine($"\tret");
    }

    private static string ShowOperand(Operand operand) => operand switch
    {
        Register => ShowRegisterOperand(),
        Imm imm => ShowImmOperand(imm),
        _ => throw new NotImplementedException()
    };

    private static string ShowRegisterOperand() => "%eax";

    private static string ShowImmOperand(Imm imm) => $"${imm.Value}";
}

using Nqcc.Assembly;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
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
        }
    }

    private void EmitMovInstruction(Mov mov)
    {
        writer.WriteLine($"\tmov\t{ShowOperand(mov.Destination)}, {ShowOperand(mov.Source)}");
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

    private static string ShowRegisterOperand() => "eax";

    private static string ShowImmOperand(Imm imm) => $"{imm.Value}";
}

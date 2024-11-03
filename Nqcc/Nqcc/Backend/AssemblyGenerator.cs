using Nqcc.Assembly;
using Nqcc.Symbols;

namespace Nqcc.Backend;

public class AssemblyGenerator(SymbolTable symbols, Tacky.Program tacky)
{
    public Program Generate()
    {
        var converter = GetTackyToAssemblyConverter(tacky);
        var converted = converter.Convert();

        var replacer = new PseudoRegisterReplacer(symbols, converted);
        var replaced = replacer.Replace();

        var fixer = new InstructionFixer(symbols, replaced);
        var @fixed = fixer.FixUp();

        return @fixed;
    }

    protected virtual TackyToAssemblyConverter GetTackyToAssemblyConverter(Tacky.Program tacky)
    {
        return new TackyToAssemblyConverter(tacky);
    }
}

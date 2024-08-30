using Nqcc.Assembly;

namespace Nqcc.Backend;

public class AssemblyGenerator(Tacky.Program tacky)
{
    public Program Generate()
    {
        var converter = new TackyToAssemblyConverter(tacky);
        var converted = converter.Convert();

        var replacer = new PseudoRegisterReplacer(converted);
        var replaced = replacer.Replace(out var lastStackSlot);

        var fixer = new InstructionFixer(replaced, lastStackSlot);
        var @fixed = fixer.FixUp();

        return @fixed;
    }
}

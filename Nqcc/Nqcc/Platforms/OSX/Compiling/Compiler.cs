﻿namespace Nqcc.Platforms.OSX.Compiling;

public class Compiler : Nqcc.Compiling.Compiler
{
    protected override Nqcc.Compiling.ICodeEmitter GetCodeEmitter(TextWriter writer) => new CodeEmitter(writer);
}

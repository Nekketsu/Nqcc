﻿namespace Nqcc;

public enum Stage
{
    Lex,
    Parse,
    Validate,
    Tacky,
    Codegen,
    Assembly,
    Object,
    Executable
}

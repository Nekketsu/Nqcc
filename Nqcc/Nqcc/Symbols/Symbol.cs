﻿namespace Nqcc.Symbols;

public abstract class Symbol(string name)
{
    public string Name { get; } = name;
}

namespace TuringCompiler.Data;

public static class OpCodes
{
    public static readonly byte IMMEDIATE0 = 0b_10_00_00_00;
    public static readonly byte IMMEDIATE1 = 0b_01_00_00_00;
    public static readonly byte ALU = 0b_00_00_00_00;
    public static readonly byte CONDITIONS = 0b_00_01_00_00;
}
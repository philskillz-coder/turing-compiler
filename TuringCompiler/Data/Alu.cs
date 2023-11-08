namespace TuringCompiler.Data;

public static class Alu
{
    public static readonly byte MOVE = 0b_00_00_00_00;
    public static readonly byte ADD = 0b_00_00_00_01;
    public static readonly byte SUBTRACT = 0b_00_00_00_10;
    public static readonly byte MULTIPLY = 0b_00_00_00_11;
    public static readonly byte DIVIDE = 0b_00_00_01_00;
    public static readonly byte NOT = 0b_00_00_01_01;
    public static readonly byte AND = 0b_00_00_01_10;
    public static readonly byte NAND = 0b_00_00_01_11;
    public static readonly byte OR = 0b_00_00_10_00;
    public static readonly byte NOR = 0b_00_00_10_01;
    public static readonly byte XOR = 0b_00_00_10_10;
    public static readonly byte XNOR = 0b_00_00_10_11;
    public static readonly byte SHR = 0b_00_00_11_00;
    public static readonly byte SHL = 0b_00_00_11_01;
}
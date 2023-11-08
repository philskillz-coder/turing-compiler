namespace TuringCompiler.Data;

public static class Conditions
{
    public static readonly string _EQUAL = "EQ";
    public static readonly string _NOTEQUAL = "NEQ";
    public static readonly string _SMALLER = "SM";
    public static readonly string _SMALLEREQUAL = "SMEQ";
    public static readonly string _GREATER = "GR";
    public static readonly string _GREATEREQUAL = "GREQ";
    public static readonly string _ALLWAYS = "ALW";
    public static readonly string _NEVER = "NVR";

    public static readonly byte EQUAL = 0b_00_00_00_00;
    public static readonly byte NOTEQUAL = 0b_00_00_00_01;
    public static readonly byte SMALLER = 0b_00_00_00_10;
    public static readonly byte SMALLEREQUAL = 0b_00_00_00_11;
    public static readonly byte GREATER = 0b_00_00_01_00;
    public static readonly byte GREATEREQUAL = 0b_00_00_01_01;
    public static readonly byte ALLWAYS = 0b_00_00_01_10;
    public static readonly byte NEVER = 0b_00_00_01_11;
    public static readonly byte JUMP_IF_RESULT = 0b_00_00_10_00;


    public static readonly Dictionary<string, int> ALL_CONDITIONS = new()
    {
        {_EQUAL, EQUAL },
        {_NOTEQUAL, NOTEQUAL },
        {_SMALLER, SMALLER },
        {_SMALLEREQUAL, SMALLEREQUAL },
        {_GREATER, GREATER },
        {_GREATEREQUAL, GREATEREQUAL },
        {_ALLWAYS, ALLWAYS },
        {_NEVER, NEVER }
    };

}
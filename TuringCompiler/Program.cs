namespace TuringCompiler;


static class Program
{
    readonly static string CODE = $@"
CONST period 0b10000000
CONST zero 0b00111111
CONST one 0b00000110
CONST two 0b01011011
CONST three 0b01001111
CONST four 0b01100110
CONST five 0b01101101
CONST six 0b01111101
CONST seven 0b00000111
CONST eight 0b01111111
CONST nine 0b01101111
SET $num 231

";


    static void Main(string[] args)
    {
        Compiler compiler = new Compiler(false);
        Console.WriteLine(string.Join("\n", compiler.Process(CODE)));
    }

    public static string Truncate(this string value, int maxChars)
    {
        return value.Length <= maxChars ? value : value.Substring(0, maxChars - 3) + "...";
    }

}
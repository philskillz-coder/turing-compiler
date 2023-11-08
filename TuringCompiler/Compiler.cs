using TuringCompiler.Data;

namespace TuringCompiler;

internal class Compiler
{
    enum Flags
    {
        IFRETURN
    }

    class Line
    {
        public Flags[]? flags { get; set; }
        public string line { get; set; }

        public Line(string line)
        {
            this.line = line;
        }

        public Line(string line, params Flags[] flags)
        {
            this.line = line;
            this.flags = flags;
        }

        public bool HasFlag(params Flags[] flags)
        {
            return flags.All(f => this.flags?.Contains(f) == true);
        }

    }

    // todo: move method -> method checks if destination already contains (immediate) value. doesnt work when moving from registers/ram
    
    private readonly ScopeManager scopeManager = new ScopeManager(new Scope("global", null));
    private readonly DefinitionManager definitionManager = new DefinitionManager();
    private readonly MemoryManager memoryManager = new MemoryManager();
    private readonly ConditionManager conditionManager = new ConditionManager();
    private readonly LoopManager loopManager = new LoopManager();

    private readonly List<Line> final = new List<Line>();

    private bool conditionRequired = false;

    public bool Do_Comments { get; set; }

    public Compiler(bool do_comments)
    {
        this.Do_Comments = do_comments;
        scopeManager.currentScope.SetVariable("RAM", new MemoryObject(false, false, Addresses.RAM));
        scopeManager.currentScope.SetVariable("CLK", new MemoryObject(false, false, Addresses.CLOCK));
        scopeManager.currentScope.SetVariable("TMP0", new MemoryObject(false, false, Addresses.RAM_TEMP_0));
        scopeManager.currentScope.SetVariable("TMP1", new MemoryObject(false, false, Addresses.RAM_TEMP_1));
        scopeManager.currentScope.SetVariable("RESL", new MemoryObject(false, false, Addresses.RESULT_LOW));
        scopeManager.currentScope.SetVariable("RESH", new MemoryObject(false, false, Addresses.RESULT_HIGH));
        scopeManager.currentScope.SetVariable("SGTGL", new MemoryObject(false, false, Addresses.SEGMENT_TOGGLE));
        scopeManager.currentScope.SetVariable("IO", new MemoryObject(false, false, Addresses.IO));
    }

    public string[] Process(string code)
    {
        string[] lines = code.Split("\n");

        foreach (string line in lines)
        {

            List<Line> compiled = ProcessLine(line);
            foreach (Line item in compiled)
            {
                final.Add(item);
            }
        }

        List<string> result = new List<string>();
        foreach (Line line in final)
        {
            result.Add(line.line);
        }

        return result.ToArray();
    }

    private List<Line> ProcessLine(string line)
    {
        line = line.Trim();
        string[] keywords = line.Split(" ");
        switch (keywords[0].Trim())
        {
            case "CONST":
                return Keyword_CONST(keywords);
            case "SET":
                return Keyword_SET(keywords);

            case "SETADR":
                return Keyword_SETADR(keywords);

            case "UNSET":
                return Keyword_UNSET(keywords);

            case "ADD":
                return Keyword_ADD(keywords);

            case "SUB":
                return Keyword_SUB(keywords);

            case "MUL":
                return Keyword_MUL(keywords);

            case "DIV":
                return Keyword_DIV(keywords);

            case "DEF":
                return Keyword_DEF(keywords);

            case "ENDDEF":
                return Keyword_ENDDEF(keywords);

            case "CALL":
                return Keyword_CALL(keywords);

            case "IF":
                return Keyword_IF(keywords);

            case "ENDIF":
                return Keyword_ENDIF(keywords);

            case "EQ":
            case "NEQ":
            case "SM":
            case "SMEQ":
            case "GR":
            case "GREQ":
            case "ALW":
            case "NVR":
                return Keyword_CONDITION(keywords);

            default:
                return new List<Line>();
        }

    }

    /* 
    Needs rework!

    $ VARIABLE
    & RAM ADDRESS
    ~ REG ADDRESS


    -----------

    VAR TARGET
    
    Create var in RAM: 1 => $A
    SET $A 1

    Set the var to value of var: $B => $A
    SET $A $B

    Set the var to value at ram address: &0 => $A
    SET $A &0

    Set the var to value at reg address: ~0 => $A
    SET $A ~0

    -----------

    RAM TARGET

    Set value at ram address: 1 => &0
    SET &0 1

    Set value at ram address to value of var: $A => &0
    SET &0 $A

    Set value at ram address to value at ram address: &1 => &0
    SET &0 &1

    Set value at ram address to value at reg address: ~0 => &0
    SET &0 ~0

    -----------

    REG TARGET

    Set value at reg address: 1 => ~0
    SET ~0 1

    Set value at reg address to value of var: $A => ~0
    SET ~0 $A

    Set value at reg address to value at ram address: &1 => ~0
    SET &0 &1

    Set value at reg address to value at reg address: ~1 => ~0
    SET ~0 ~1

    -----------

    RESET VAR ADDRESSES

    Set address of var to var address: $A now points to $B
    SETADR $A $B

    Set address of var to ram address: $A now points to &1
    SETADR $A &1

    Set address of var to reg address: $A now points to ~1
    SETADR $A ~1

    */

    private List<Line> Keyword_CONST(string[] keywords)
    {
        List<Line> compiled = new List<Line>();

        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        string name = keywords[1].Trim();
        string value = keywords[2].Trim();

        if (!Parse.Int(value, out int valueInt))
        {
            throw new Exception("Value not int");
        }
        
        if (scopeManager.currentScope.ResolveVarName(name, true, out _))
        {
            throw new Exception($"CONST {name} already exists");
        }

        scopeManager.currentScope.SetVariable(name, new MemoryObject(false, true, valueInt));
        
        return compiled;
    }
    
    private List<Line> Keyword_SET(string[] keywords)
    {
        List<Line> compiled = new List<Line>();

        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        string rawAddress = keywords[1].Trim();
        string rawValue = keywords[2].Trim();

        if (!scopeManager.currentScope.ResolveAddress(
            rawAddr: rawValue,
            in_parents: true,
            out MemoryObject? value
        ) || value == null)
        {
            throw new Exception("Value could not be resolved");
        }

        // Creates variable if not exists / Sets rawAddress to variable address
        if (rawAddress.StartsWith(Styles.VARIABLE))
        {
            MemoryObject? address;
            string varName = rawAddress.Substring(Styles.VARIABLE.Length);
            if (!scopeManager.currentScope.VariableExists(
                variable: varName,
                in_parents: true
            ))
            {
                if (!memoryManager.GetFreeRamAddress(out address) || address == null)
                {
                    throw new Exception("Out of memory");
                }

                scopeManager.currentScope.SetVariable(varName, address);
            } else
            {
                if (!scopeManager.currentScope.ResolveVarName(varName, true, out address) || address == null)
                {
                    throw new Exception("Could not resolve variable");
                }
            }

            if (address.InRam && !address.IsImmediate) {
                rawAddress = $"&{address.Value}";
            }
            else if (!address.InRam && !address.IsImmediate)
            {
                rawAddress = $"~{address.Value}";
            } else
            {
                throw new Exception("Variable neither in ram nor in reg");
            }
        }

        if (rawAddress.StartsWith(Styles.RAM_ADDR))
        {
            MemoryObject? address;
            string rawRamAddr = rawAddress.Substring(Styles.RAM_ADDR.Length);

            if (!scopeManager.currentScope.ResolveRamAddr(rawRamAddr, out address) || address == null)
            {
                throw new Exception("Could not resolve ram address");
            }

            if (value.InRam && !value.IsImmediate) // RAM->RAM
            {
                // set ram address (RAM)
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {value.Value} 0 {Addresses.RAM_ADDRESS}"
                ));
                // move RAM->TEMP
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE} {Addresses.RAM} 0 {Addresses.RAM_TEMP_0}"
                ));

                // set ram address (Var)
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {address.Value} 0 {Addresses.RAM_ADDRESS}"
                ));
                // move TEMP->Var(RAM)
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE} {Addresses.RAM_TEMP_0} 0 {Addresses.RAM}"
                ));
            }
            else if (!value.InRam && !value.IsImmediate) // REG->RAM
            {
                // set ram address (Var)
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {address.Value} 0 {Addresses.RAM_ADDRESS}"
                ));

                // move REG->Var(RAM)
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE} {value.Value} 0 {Addresses.RAM}"
                ));
            }
            else if (!value.InRam && value.IsImmediate) // IME->RAM
            {
                // set ram address (Var)
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {address.Value} 0 {Addresses.RAM_ADDRESS}"
                ));

                // move IME->Var(RAM)
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {value.Value} 0 {Addresses.RAM}"
                ));
            }
        }

        if (rawAddress.StartsWith(Styles.REG_ADDR))
        {

            MemoryObject? address;
            string rawRegAddr = rawAddress.Substring(Styles.REG_ADDR.Length);

            if (!scopeManager.currentScope.ResolveRegAddr(rawRegAddr, out address) || address == null)
            {
                throw new Exception("Could not resolve reg address");
            }

            if (value.InRam && !value.IsImmediate) // RAM->REG
            {
                // set ram address (RAM)
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {value.Value} 0 {Addresses.RAM_ADDRESS}"
                ));
                // move RAM->REG
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE} {Addresses.RAM} 0 {address.Value}"
                ));
            }
            else if (!value.InRam && !value.IsImmediate) // REG->REG
            {
                // move REG->REG
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE} {value.Value} 0 {address.Value}"
                ));
            }
            else if (!value.InRam && value.IsImmediate) // IME->REG
            {
                // move IME->REG
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {value.Value} 0 {address.Value}"
                ));

            }
        }

        return compiled;
    }

    private List<Line> Keyword_SETADR(string[] keywords)
    {
        List<Line> compiled = new List<Line>();

        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        string rawAddress = keywords[1].Trim();
        string rawValue = keywords[2].Trim();

        string addressVarName = rawAddress.Substring(Styles.VARIABLE.Length);

        MemoryObject? value;
        if (!scopeManager.currentScope.ResolveAddress(rawValue, true, out value) || value == null || value.IsImmediate)
        {
            throw new Exception("Could not resolve variable");
        }

        scopeManager.currentScope.SetVariable(addressVarName, value);

        return compiled;
    }

    private List<Line> Keyword_UNSET(string[] keywords)
    {
        List<Line> compiled = new List<Line>();

        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        string[] variables = keywords.Skip(1).ToArray();

        foreach (string variable in variables)
        {
            if (!variable.StartsWith(Styles.VARIABLE))
            {
                throw new Exception("Not a variable");
            }

            if (!scopeManager.currentScope.ResolveAddress(
                rawAddr: variable,
                in_parents: false,
                out MemoryObject? value
            ) || value == null)
            {
                throw new Exception("Memory address could not be resolved");
            }

            scopeManager.currentScope.UnsetVariable(variable.Substring(1));
        }

        return compiled;
    }

    private List<Line> Keyword_ADD(string[] keywords)
    {
        List<Line> compiled = new List<Line>();

        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        var _argument1 = keywords[1].Trim();
        var _argument2 = keywords[2].Trim();
        var _result = keywords[3].Trim();

        if (!scopeManager.currentScope.ResolveAddress(_argument1, true, out MemoryObject? argument0) || argument0 == null)
        {
            throw new Exception("Memory address could not be resolved");
        }

        // get value for variable
        if (!scopeManager.currentScope.ResolveAddress(_argument2, true, out MemoryObject? argument1) || argument1 == null)
        {
            throw new Exception("Memory address could not be resolved");
        }

        // get value for variable
        if (!scopeManager.currentScope.ResolveAddress(_result, true, out MemoryObject? result) || result == null || result.IsImmediate)
        {
            throw new Exception("Memory address could not be resolved");
        }

        bool argument0Immediate = false;
        bool argument1Immediate = false;

        int value0 = -1;
        int value1 = -1;
        int resultk = -1;

        // argument 0
        if (argument0.IsImmediate)
        {
            argument0Immediate = true;
            value0 = argument0.Value;
        }
        if (!argument0.InRam)
        {
            value0 = argument0.Value;
        }
        if (argument0.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument0.Value} 0 {Addresses.RAM_ADDRESS}"
            ));

            // move value from ram
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_0}"
            ));

            value0 = Addresses.RAM_TEMP_0;
        }

        // argument 1
        if (argument1.IsImmediate)
        {
            argument1Immediate = true;
            value1 = argument1.Value;
        }
        if (!argument1.InRam)
        {
            value1 = argument1.Value;
        }
        if (argument1.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument1.Value} 0 {Addresses.RAM_ADDRESS}"
            ));

            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_1}"
            ));

            value1 = Addresses.RAM_TEMP_1;
        }

        // result
        if (!result.InRam)
        {
            resultk = result.Value;
        }
        if (result.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {result.Value} 0 {Addresses.RAM_ADDRESS}"
            ));
            resultk = Addresses.RAM;
        }

        compiled.Add(new Line(
            $"{OpCodes.ALU}+{Alu.ADD}{(argument0Immediate ? "+"+OpCodes.IMMEDIATE0 : "+"+0)}{(argument1Immediate ? "+"+OpCodes.IMMEDIATE1 : "+"+0)} {value0} {value1} {resultk}"
        ));

        return compiled;
    }

    private List<Line> Keyword_SUB(string[] keywords)
    {
        List<Line> compiled = new List<Line>();

        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        var _argument1 = keywords[1].Trim();
        var _argument2 = keywords[2].Trim();
        var _result = keywords[3].Trim();

        if (!scopeManager.currentScope.ResolveAddress(_argument1, true, out MemoryObject? argument0) || argument0 == null)
        {
            throw new Exception("Memory address could not be resolved");
        }

        // get value for variable
        if (!scopeManager.currentScope.ResolveAddress(_argument2, true, out MemoryObject? argument1) || argument1 == null)
        {
            throw new Exception("Memory address could not be resolved");
        }

        // get value for variable
        if (!scopeManager.currentScope.ResolveAddress(_result, true, out MemoryObject? result) || result == null || result.IsImmediate)
        {
            throw new Exception("Memory address could not be resolved");
        }

        bool argument0Immediate = false;
        bool argument1Immediate = false;

        int value0 = -1;
        int value1 = -1;
        int resultk = -1;

        // argument 0
        if (argument0.IsImmediate)
        {
            argument0Immediate = true;
            value0 = argument0.Value;
        }
        if (!argument0.InRam)
        {
            value0 = argument0.Value;
        }
        if (argument0.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument0.Value} 0 {Addresses.RAM_ADDRESS}"
            ));

            // move value from ram
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_0}"
            ));

            value0 = Addresses.RAM_TEMP_0;
        }

        // argument 1
        if (argument1.IsImmediate)
        {
            argument1Immediate = true;
            value1 = argument1.Value;
        }
        if (!argument1.InRam)
        {
            value1 = argument1.Value;
        }
        if (argument1.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument1.Value} 0 {Addresses.RAM_ADDRESS}"
            ));

            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_1}"
            ));

            value1 = Addresses.RAM_TEMP_1;
        }

        // result
        if (!result.InRam)
        {
            resultk = result.Value;
        }
        if (result.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {result.Value} 0 {Addresses.RAM_ADDRESS}"
            ));
            resultk = Addresses.RAM;
        }

        compiled.Add(new Line(
            $"{OpCodes.ALU}+{Alu.SUBTRACT}{(argument0Immediate ? "+"+OpCodes.IMMEDIATE0 : "+"+0)}{(argument1Immediate ? "+"+OpCodes.IMMEDIATE1 : "+"+0)} {value0} {value1} {resultk}"
        ));

        return compiled;
    }

    private List<Line> Keyword_MUL(string[] keywords)
    {
        List<Line> compiled = new List<Line>();

        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        var level = keywords[1].Trim().ToUpper();
        var _argument1 = keywords[2].Trim();
        var _argument2 = keywords[3].Trim();
        var _result = keywords[4].Trim();

        if (!scopeManager.currentScope.ResolveAddress(_argument1, true, out MemoryObject? argument0) || argument0 == null)
        {
            throw new Exception("Memory address could not be resolved");
        }

        // get value for variable
        if (!scopeManager.currentScope.ResolveAddress(_argument2, true, out MemoryObject? argument1) || argument1 == null)
        {
            throw new Exception("Memory address could not be resolved");
        }

        // get value for variable
        if (!scopeManager.currentScope.ResolveAddress(_result, true, out MemoryObject? result) || result == null || result.IsImmediate)
        {
            throw new Exception("Memory address could not be resolved");
        }

        bool argument0Immediate = false;
        bool argument1Immediate = false;

        int value0 = -1;
        int value1 = -1;
        int resultk = -1;

        // argument 0
        if (argument0.IsImmediate)
        {
            argument0Immediate = true;
            value0 = argument0.Value;
        }
        if (!argument0.InRam)
        {
            value0 = argument0.Value;
        }
        if (argument0.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument0.Value} 0 {Addresses.RAM_ADDRESS}"
            ));

            // move value from ram
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_0}"
            ));

            value0 = Addresses.RAM_TEMP_0;
        }

        // argument 1
        if (argument1.IsImmediate)
        {
            argument1Immediate = true;
            value1 = argument1.Value;
        }
        if (!argument1.InRam)
        {
            value1 = argument1.Value;
        }
        if (argument1.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument1.Value} 0 {Addresses.RAM_ADDRESS}"
            ));

            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_1}"
            ));

            value1 = Addresses.RAM_TEMP_1;
        }

        // result
        if (!result.InRam)
        {
            resultk = result.Value;
        }
        if (result.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {result.Value} 0 {Addresses.RAM_ADDRESS}"
            ));
            resultk = Addresses.RAM;
        }

        if (level == "LOW")
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MULTIPLY}{(argument0Immediate ? "+" + OpCodes.IMMEDIATE0 : "+" + 0)}{(argument1Immediate ? "+" + OpCodes.IMMEDIATE1 : "+" + 0)} {value0} {value1} {resultk}"
            ));
        }
        else if (level == "HIGH")
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MULTIPLY}+32{(argument0Immediate ? "+" + OpCodes.IMMEDIATE0 : "+" + 0)}{(argument1Immediate ? "+" + OpCodes.IMMEDIATE1 : "+" + 0)} {value0} {value1} {resultk}"
            ));
        }

        return compiled;
    }
    
    private List<Line> Keyword_DIV(string[] keywords)
    {
        List<Line> compiled = new List<Line>();

        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        var level = keywords[1].Trim().ToUpper();
        var _argument1 = keywords[2].Trim();
        var _argument2 = keywords[3].Trim();
        var _result = keywords[4].Trim();

        if (!scopeManager.currentScope.ResolveAddress(_argument1, true, out MemoryObject? argument0) || argument0 == null)
        {
            throw new Exception("Memory address could not be resolved");
        }

        // get value for variable
        if (!scopeManager.currentScope.ResolveAddress(_argument2, true, out MemoryObject? argument1) || argument1 == null)
        {
            throw new Exception("Memory address could not be resolved");
        }

        // get value for variable
        if (!scopeManager.currentScope.ResolveAddress(_result, true, out MemoryObject? result) || result == null || result.IsImmediate)
        {
            throw new Exception("Memory address could not be resolved");
        }

        bool argument0Immediate = false;
        bool argument1Immediate = false;

        int value0 = -1;
        int value1 = -1;
        int resultk = -1;

        // argument 0
        if (argument0.IsImmediate)
        {
            argument0Immediate = true;
            value0 = argument0.Value;
        }
        if (!argument0.InRam)
        {
            value0 = argument0.Value;
        }
        if (argument0.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument0.Value} 0 {Addresses.RAM_ADDRESS}"
            ));

            // move value from ram
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_0}"
            ));

            value0 = Addresses.RAM_TEMP_0;
        }

        // argument 1
        if (argument1.IsImmediate)
        {
            argument1Immediate = true;
            value1 = argument1.Value;
        }
        if (!argument1.InRam)
        {
            value1 = argument1.Value;
        }
        if (argument1.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument1.Value} 0 {Addresses.RAM_ADDRESS}"
            ));

            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_1}"
            ));

            value1 = Addresses.RAM_TEMP_1;
        }

        // result
        if (!result.InRam)
        {
            resultk = result.Value;
        }
        if (result.InRam)
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {result.Value} 0 {Addresses.RAM_ADDRESS}"
            ));
            resultk = Addresses.RAM;
        }

        if (level == "LOW")
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.DIVIDE}{(argument0Immediate ? "+" + OpCodes.IMMEDIATE0 : "+" + 0)}{(argument1Immediate ? "+" + OpCodes.IMMEDIATE1 : "+" + 0)} {value0} {value1} {resultk}"
            ));
        }
        else if (level == "HIGH")
        {
            compiled.Add(new Line(
                $"{OpCodes.ALU}+{Alu.DIVIDE}+32{(argument0Immediate ? "+" + OpCodes.IMMEDIATE0 : "+" + 0)}{(argument1Immediate ? "+" + OpCodes.IMMEDIATE1 : "+" + 0)} {value0} {value1} {resultk}"
            ));
        }

        return compiled;
    }

    private List<Line> Keyword_DEF(string[] keywords)
    {
        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        string name = keywords[1].Trim();
        int currentPosition = (final.Count + 1) * 4;
        if(!definitionManager.CreateDefinition(name, final.Count, currentPosition, out Definition? definition) || definition == null)
        {
            throw new Exception("A definition with this name already exists");
        }

        if (!scopeManager.CreateScope("definition." + name, scopeManager.currentScope, out Scope? scope) || scope == null)
        {
            throw new Exception("Scope error");
        }
        scopeManager.SetCurrentScope(scope);
        definitionManager.PushDefinition(definition);

        return new List<Line>();
    }

    private List<Line> Keyword_ENDDEF(string[] keywords)
    {
        List<Line> compiled = new List<Line>();

        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        if (!definitionManager.CurrentDefinition(out Definition? definition) || definition == null)
        {
            throw new Exception("Not in a definition");
        }

        //                                      Insertion   Alu move
        int currentPosition = (final.Count * 4) + 4         + 4;
        definitionManager.PopDefinition();

        // inject a jump-to reference before the definition start to jump to the instruction where the definition ends
        final.Insert(
            definition.ResultStartIndex,
            new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {currentPosition} 0 {Addresses.CLOCK}"
            )
        );

        // jump-back to origin
        compiled.Add(new Line(
            $"{OpCodes.ALU}+{Alu.MOVE} {Addresses.STACK} 0 {Addresses.CLOCK}"
        ));

        return compiled;
    }

    private List<Line> Keyword_CALL(string[] keywords)
    {
        List<Line> compiled = new List<Line>();

        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        string name = keywords[1].Trim();

        if (!definitionManager.ResolveDefinition(name, out Definition? definition) || definition == null)
        {
            throw new Exception("Definition not found");
        }

        // +2 for 2 actions before in the code ( because result has not been added to final -> final.Count is 2 less than it should be)
        //jump-back address
        compiled.Add(new Line(
            $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {(final.Count + 2) * 4} 0 {Addresses.STACK}"
        ));

        compiled.Add(new Line(
            $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {definition.StartInstruction} 0 {Addresses.CLOCK}"
        ));


        return compiled;
    }

    private List<Line> Keyword_IF(string[] keywords)
    {
        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        // actual condition to check (example: (IF) GR $A 1)
        string conditionCode = string.Join(" ", keywords.Skip(1).ToList());
        conditionRequired = true;
        List<Line> ifInstructions = ProcessLine(conditionCode);
        conditionRequired = false;
        int currentPosition = (final.Count + ifInstructions.Count() + 1) * 4;

        foreach (Line item in ifInstructions)
        {
            if (item.HasFlag(Flags.IFRETURN))
            {
                final.Add(new Line(
                    item.line.Replace("{pos}", currentPosition.ToString())
                ));
            } else
            {
                final.Add(item);
            }
        }

        conditionManager.CreateCondition(final.Count, currentPosition + ifInstructions.Count(), out Condition? condition);
        conditionManager.OpenCondition(condition);

        return new List<Line>();
    }

    private List<Line> Keyword_ENDIF(string[] keywords)
    {
        if (conditionRequired)
        {
            throw new Exception("This is not a condition");
        }

        if (!conditionManager.CurrentCondition(out Condition? condition) || condition == null)
        {
            throw new Exception("Not in a IF statement");
        }

        // +1 for 1 extra action
        int currentPosition = (final.Count + 1) * 4;
        conditionManager.CloseCurrentCondition();

        // inject a jump-to reference before the definition start to jump to the instruction where the definition ends
        final.Insert(
            condition.ResultStartIndex,
            new Line(
                $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {currentPosition} 0 {Addresses.CLOCK}"
            )
        );

        return new List<Line>();
    }

    private List<Line> Keyword_CONDITION(string[] keywords)
    {
        List<Line> compiled = new List<Line>();

        if (!Conditions.ALL_CONDITIONS.TryGetValue(keywords[0].Trim(), out int condition))
        {
            throw new Exception("Condition not found");
        }

        if (conditionRequired)
        {
            var _argument0 = keywords[1].Trim();
            var _argument1 = keywords[2].Trim();

            // get value for variable
            if (!scopeManager.currentScope.ResolveAddress(_argument0, true, out MemoryObject? argument0) || argument0 == null)
            {
                throw new Exception("Memory address could not be resolved");
            }

            // get value for variable
            if (!scopeManager.currentScope.ResolveAddress(_argument1, true, out MemoryObject? argument1) || argument1 == null)
            {
                throw new Exception("Memory address could not be resolved");
            }


            bool argument0Immediate = false;
            bool argument1Immediate = false;

            int value0 = -1;
            int value1 = -1;

            // argument 0
            if (argument0.IsImmediate)
            {
                argument0Immediate = true;
                value0 = argument0.Value;
            }
            if (!argument0.InRam) // one of the registers
            {
                value0 = argument0.Value;
            }
            if (argument0.InRam)
            {
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument0.Value} 0 {Addresses.RAM_ADDRESS}"
                ));

                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_0}"
                ));
                value0 = Addresses.RAM_TEMP_0;
            }

            // argument 1
            if (argument1.IsImmediate)
            {
                argument1Immediate = true;
                value1 = argument1.Value;
            }
            if (!argument1.InRam) // one of the registers
            {
                value1 = argument1.Value;
            }
            if (argument1.InRam)
            {
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument1.Value} 0 {Addresses.RAM_ADDRESS}"
                ));

                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_1}"
                ));
                value1 = Addresses.RAM_TEMP_1;
            }

            compiled.Add(new Line(
                $"{OpCodes.CONDITIONS}+{condition}+{Conditions.JUMP_IF_RESULT}{(argument0Immediate ? "+"+OpCodes.IMMEDIATE0 : "+"+0)}{(argument1Immediate ? "+"+OpCodes.IMMEDIATE1 : "+"+0)} {value0} {value1} {{pos}}",
                Flags.IFRETURN
            ));

        }
        else
        {
            var _argument0 = keywords[1].Trim();
            var _argument1 = keywords[2].Trim();
            var _result = keywords[3].Trim();

            // get value for variable
            if (!scopeManager.currentScope.ResolveAddress(_argument0, true, out MemoryObject? argument0) || argument0 == null)
            {
                throw new Exception("Memory address could not be resolved");
            }

            // get value for variable
            if (!scopeManager.currentScope.ResolveAddress(_argument1, true, out MemoryObject? argument1) || argument1 == null)
            {
                throw new Exception("Memory address could not be resolved");
            }

            if (!scopeManager.currentScope.ResolveAddress(_result, true, out MemoryObject? result) || result == null || result.IsImmediate)
            {
                throw new Exception("Memory address could not be resolved");
            }

            bool argument0Immediate = false;
            bool argument1Immediate = false;

            int value0 = -1;
            int value1 = -1;
            int resultk = -1;

            // argument 0
            if (argument0.IsImmediate)
            {
                argument0Immediate = true;
                value0 = argument0.Value;
            }
            if (!argument0.InRam) // one of the registers
            {
                value0 = argument0.Value;
            }
            if (argument0.InRam)
            {
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument0.Value} 0 {Addresses.RAM_ADDRESS}"
                ));
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_0}"
                ));

                value0 = Addresses.RAM_TEMP_0;
            }

            // argument 1
            if (argument1.IsImmediate)
            {
                argument1Immediate = true;
                value1 = argument1.Value;
            }
            if (!argument1.InRam) // one of the registers
            {
                value1 = argument1.Value;
            }
            if (argument1.InRam)
            {
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {argument1.Value} 0 {Addresses.RAM_ADDRESS}"
                ));
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE} 0 0 {Addresses.RAM_TEMP_1}"
                ));

                value1 = Addresses.RAM_TEMP_1;
            }

            // result
            if (!result.InRam) // one of the registers
            {
                resultk = result.Value;
            }
            if (result.InRam)
            {
                compiled.Add(new Line(
                    $"{OpCodes.ALU}+{Alu.MOVE}+{OpCodes.IMMEDIATE0} {result.Value} 0 {Addresses.RAM_ADDRESS}"
                ));
                resultk = Addresses.RAM;
            }

            compiled.Add(new Line(
                $"{OpCodes.CONDITIONS}+{condition}+{Conditions.JUMP_IF_RESULT}{(argument0Immediate ? "+"+OpCodes.IMMEDIATE0 : "+"+0)}{(argument1Immediate ? "+"+OpCodes.IMMEDIATE1 : "+"+0)} {value0} {value1} {resultk}"
            ));
        }

        return compiled;
    }
}
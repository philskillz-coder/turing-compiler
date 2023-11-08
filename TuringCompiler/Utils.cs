using TuringCompiler.Data;

namespace TuringCompiler;

public class MemoryObject
{
    public bool InRam { get; set; }
    public bool IsImmediate { get; set; }
    public int Value { get; set; }

    /// <summary>
    /// Creates a new MemoryObject representing a RAM memory with the specified value.
    /// </summary>
    /// <param name="value">The value of the RAM memory.</param>
    /// <returns>A new MemoryObject representing a RAM memory.</returns>
    public static MemoryObject RAM(int value)
    {
        return new MemoryObject(inRam: true, isImmediate: false, value: value);
    }

    /// <summary>
    /// Creates a new MemoryObject representing a register with the specified value.
    /// </summary>
    /// <param name="value">The value of the register.</param>
    /// <returns>A new MemoryObject representing a register.</returns>
    public static MemoryObject REG(int value)
    {
        return new MemoryObject(inRam: false, isImmediate: false, value: value);
    }

    /// <summary>
    /// Creates a new MemoryObject representing an immediate value with the specified value.
    /// </summary>
    /// <param name="value">The value of the immediate value.</param>
    /// <returns>A new MemoryObject representing an immediate value.</returns>
    public static MemoryObject IM(int value)
    {
        return new MemoryObject(inRam: false, isImmediate: true, value: value);
    }

    /// <summary>
    /// Private constructor for MemoryObject.
    /// </summary>
    /// <param name="inRam">Indicates if the MemoryObject is in RAM.</param>
    /// <param name="isImmediate">Indicates if the MemoryObject is an immediate value.</param>
    /// <param name="value">The value of the MemoryObject.</param>
    /// <exception cref="ArgumentException">Thrown when MemoryObject is both in RAM and an immediate value.</exception>
    public MemoryObject(bool inRam, bool isImmediate, int value)
    {
        if (inRam && isImmediate)
        {
            throw new ArgumentException("MemoryObject can't be in RAM and an immediate value.");
        }

        this.InRam = inRam;
        this.IsImmediate = isImmediate;
        this.Value = value;
    }


}

public class Definition
{
    public int ResultStartIndex { get; set; }
    public int StartInstruction { get; set; }

    public int? EndInstruction { get; set; }

    /// <summary>
    /// Initializes a new instance of the Definition class with the specified result start index and start instruction.
    /// </summary>
    /// <param name="resultStartIndex">The result start index of the definition.</param>
    /// <param name="startInstruction">The start instruction of the definition.</param>
    public Definition(int resultStartIndex, int startInstruction)
    {
        ResultStartIndex = resultStartIndex;
        StartInstruction = startInstruction;
        EndInstruction = null;
    }

}

public class DefinitionManager
{
    private readonly Dictionary<string, Definition> definitions = new Dictionary<string, Definition>();
    private readonly List<Definition> definitionStack = new List<Definition>();

    public DefinitionManager() { }

    /// <summary>
    /// Resolves the definition with the specified raw definition string.
    /// </summary>
    /// <param name="rawDefinition">The raw definition string to resolve.</param>
    /// <param name="definition">The resolved Definition object, if successful.</param>
    /// <returns>True if the definition was successfully resolved, false otherwise.</returns>
    public bool ResolveDefinition(string rawDefinition, out Definition? definition)
    {
        return definitions.TryGetValue(rawDefinition, out definition);
    }

    /// <summary>
    /// Checks if a definition with the specified name exists.
    /// </summary>
    /// <param name="definitionName">The name of the definition to check.</param>
    /// <returns>True if the definition exists, false otherwise.</returns>
    public bool DefinitionExists(string definitionName)
    {
        return definitions.ContainsKey(definitionName);
    }

    /// <summary>
    /// Creates a new definition with the specified name, result start index, and start instruction.
    /// </summary>
    /// <param name="name">The name of the definition.</param>
    /// <param name="resultStartIndex">The result start index of the definition.</param>
    /// <param name="startInstruction">The start instruction of the definition.</param>
    /// <param name="definition">The created Definition object, if successful.</param>
    /// <returns>True if the definition was successfully created, false if a definition with the same name already exists.</returns>
    public bool CreateDefinition(string name, int resultStartIndex, int startInstruction, out Definition? definition)
    {
        if (definitions.ContainsKey(name))
        {
            definition = null;
            return false;
        }

        definition = new Definition(resultStartIndex, startInstruction);
        definitions[name] = definition;
        return true;
    }

    /// <summary>
    /// Pushes the specified definition onto the definition stack.
    /// </summary>
    /// <param name="definition">The definition to push.</param>
    public void PushDefinition(Definition definition)
    {
        definitionStack.Add(definition);
    }

    /// <summary>
    /// Retrieves the current definition from the top of the definition stack.
    /// </summary>
    /// <returns>The current definition.</returns>
    public bool CurrentDefinition(out Definition? definition)
    {
        definition = definitionStack.LastOrDefault();
        return definition != null;
    }

    /// <summary>
    /// Pops the current definition from the top of the definition stack.
    /// </summary>
    public void PopDefinition()
    {
        definitionStack.RemoveAt(definitionStack.Count - 1);
    }

    /// <summary>
    /// Returns the size of the definition stack.
    /// </summary>
    /// <returns>The size of the definition stack.</returns>
    public int DefinitionStackSize()
    {
        return definitionStack.Count;
    }


}


public class Loop
{
    public int ResultStartIndex { get; set; }
    public int StartInstruction { get; set; }

    public int? EndInstruction { get; set; }


    /// <summary>
    /// Initializes a new instance of the Loop class with the specified result start index and start instruction.
    /// </summary>
    /// <param name="resultStartIndex">The result start index of the loop.</param>
    /// <param name="startInstruction">The start instruction of the loop.</param>
    public Loop(int resultStartIndex, int startInstruction)
    {
        ResultStartIndex = resultStartIndex;
        StartInstruction = startInstruction;
        EndInstruction = null;
    }
}

public class LoopManager
{
    private readonly List<Loop> openLoops = new List<Loop>();

    public LoopManager() { }


    /// <summary>
    /// Creates a new loop object with the specified result start index and start instruction.
    /// </summary>
    /// <param name="resultStartIndex">The result start index of the loop.</param>
    /// <param name="startInstruction">The start instruction of the loop.</param>
    /// <param name="loop">The created Loop object.</param>
    public void CreateLoop(int resultStartIndex, int startInstruction, out Loop loop)
    {
        loop = new Loop(resultStartIndex, startInstruction);
    }

    /// <summary>
    /// Opens the specified loop by adding it to the list of open loops.
    /// </summary>
    /// <param name="loop">The loop to open.</param>
    public void OpenLoop(Loop loop)
    {
        openLoops.Add(loop);
    }

    /// <summary>
    /// Retrieves the current open loop from the list of open loops.
    /// </summary>
    /// <returns>The current open loop.</returns>
    public Loop CurrentLoop()
    {
        return openLoops.Last();
    }

    /// <summary>
    /// Closes the current open loop by removing it from the list of open loops.
    /// </summary>
    public void CloseLoop()
    {
        openLoops.RemoveAt(openLoops.Count - 1);
    }

    /// <summary>
    /// Returns the count of open loops.
    /// </summary>
    /// <returns>The count of open loops.</returns>
    public int OpenLoopsCount()
    {
        return openLoops.Count;
    }



}


public class Condition
{
    public int ResultStartIndex { get; set; }
    public int StartInstruction { get; set; }

    public int? EndInstruction { get; set; }

    /// <summary>
    /// Initializes a new instance of the Condition class with the specified result start index and start instruction.
    /// </summary>
    /// <param name="resultStartIndex">The result start index of the condition.</param>
    /// <param name="startInstruction">The start instruction of the condition.</param>
    public Condition(int resultStartIndex, int startInstruction)
    {
        ResultStartIndex = resultStartIndex;
        StartInstruction = startInstruction;
        EndInstruction = null;
    }
}

public class ConditionManager
{
    private readonly List<Condition> openConditions = new List<Condition>();

    public ConditionManager() { }



    /// <summary>
    /// Creates a new condition object with the specified result start index and start instruction.
    /// </summary>
    /// <param name="resultStartIndex">The result start index of the condition.</param>
    /// <param name="startInstruction">The start instruction of the condition.</param>
    /// <param name="condition">The created Condition object.</param>
    public void CreateCondition(int resultStartIndex, int startInstruction, out Condition condition)
    {
        condition = new Condition(resultStartIndex, startInstruction);
    }

    /// <summary>
    /// Adds the specified condition to the list of open code conditions.
    /// </summary>
    /// <param name="condition">The condition to add.</param>
    public void OpenCondition(Condition condition)
    {
        openConditions.Add(condition);
    }

    /// <summary>
    /// Retrieves the latest open code condition from the list of open code conditions.
    /// </summary>
    /// <returns>The latest open code condition.</returns>
    public bool CurrentCondition(out Condition? condition)
    {
        condition = openConditions.LastOrDefault();
        return condition != null;
    }

    /// <summary>
    /// Closes the latest open code condition by removing it from the list of open code conditions.
    /// </summary>
    public void CloseCurrentCondition()
    {
        openConditions.RemoveAt(openConditions.Count - 1);
    }

    /// <summary>
    /// Returns the count of open code conditions.
    /// </summary>
    /// <returns>The count of open code conditions.</returns>
    public int OpenConditionsCount()
    {
        return openConditions.Count;
    }



}

public class Scope
{
    private readonly Dictionary<string, MemoryObject> variables = new Dictionary<string, MemoryObject>();
    public readonly string name;
    public readonly Scope? parent;

    /// <summary>
    /// Initializes a new instance of the Scope class with the specified name and parent scope.
    /// </summary>
    /// <param name="name">The name of the scope.</param>
    /// <param name="parent">The parent scope.</param>
    public Scope(string name, Scope? parent)
    {
        this.name = name;
        this.parent = parent;
    }

    /// <summary>
    /// Checks if the specified variable exists in the current class's dictionary and, if enabled, in the parent classes.
    /// </summary>
    /// <param name="variable">The variable to check for existence.</param>
    /// <param name="in_parents">Flag indicating whether to check for existence in parent classes.</param>
    /// <returns>True if the variable exists, false otherwise.</returns>
    public bool VariableExists(string variable, bool in_parents)
    {
        if (variables.ContainsKey(variable) || (in_parents && parent?.VariableExists(variable, true) == true))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves the given variable name, considering the current class's dictionary and, if enabled, variable names in parent classes.
    /// </summary>
    /// <param name="variable">The variable name to resolve.</param>
    /// <param name="in_parents">Flag indicating whether to resolve variable names in parent classes.</param>
    /// <param name="resolved">The resolved MemoryObject, if successful.</param>
    /// <returns>True if the variable was successfully resolved, false otherwise.</returns>
    public bool ResolveVarName(string variable, bool in_parents, out MemoryObject? resolved)
    {
        if (variables.TryGetValue(variable, out resolved) || (in_parents && parent?.ResolveVarName(variable, true, out resolved) == true)) {
            return true;
        } else
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves the given memory address string as a RAM address.
    /// </summary>
    /// <param name="addr">The memory address string to resolve.</param>
    /// <param name="resolved">The resolved MemoryObject, if successful.</param>
    /// <returns>True if the memory address was successfully resolved, false otherwise.</returns>
    public bool ResolveRamAddr(string addr, out MemoryObject? resolved)
    {
        var isResolved = Parse.Int(addr, out int _value);
        resolved = MemoryObject.RAM(_value);
        return isResolved;
    }

    /// <summary>
    /// Resolves the given memory address string as a register address.
    /// </summary>
    /// <param name="addr">The memory address string to resolve.</param>
    /// <param name="resolved">The resolved MemoryObject, if successful.</param>
    /// <returns>True if the register address was successfully resolved, false otherwise.</returns>
    public bool ResolveRegAddr(string addr, out MemoryObject? resolved)
    {
        var isResolved = Parse.Int(addr, out int _value);
        resolved = MemoryObject.REG(_value);
        return isResolved;
    }

    /// <summary>
    /// Resolves the given raw address string into a MemoryObject, considering variable names, memory addresses, and register addresses.
    /// </summary>
    /// <param name="rawAddr">The raw address string to resolve.</param>
    /// <param name="in_parents">Flag indicating whether to resolve variable names in parent classes.</param>
    /// <param name="resolved">The resolved MemoryObject, if successful.</param>
    /// <returns>True if the address was successfully resolved, false otherwise.</returns>
    public bool ResolveAddress(string rawAddr, bool in_parents, out MemoryObject? resolved)
    {
        bool isResolved = false;
        if (rawAddr.StartsWith(Styles.VARIABLE))
        {
            isResolved = ResolveVarName(rawAddr.Substring(1), in_parents, out resolved);
            return isResolved;
        }
        else if (rawAddr.StartsWith(Styles.RAM_ADDR))
        {
            isResolved = ResolveRamAddr(rawAddr.Substring(1), out resolved);
            return isResolved;
        }
        else if (rawAddr.StartsWith(Styles.REG_ADDR))
        {
            isResolved = ResolveRegAddr(rawAddr.Substring(1), out resolved);
            return isResolved;
        }

        isResolved = Parse.Int(rawAddr, out int _value);
        resolved = MemoryObject.IM(_value);
        return isResolved;
    }

    /// <summary>
    /// Sets the value of a variable with the specified name.
    /// </summary>
    /// <param name="name">The name of the variable to set.</param>
    /// <param name="address">The memory object representing the value to set.</param>
    /// <returns>True if the variable already existed and its value was updated, false if a new variable was added.</returns>
    public bool SetVariable(string name, MemoryObject address)
    {
        if (!variables.ContainsKey(name))
        {
            variables.Add(name, address);
            return false;
        }
        variables[name] = address;
        return true;
    }

    public bool UnsetVariable(string name)
    {
        return variables.Remove(name);
    }

}

public class ScopeManager
{
    public Scope currentScope;
    private readonly Dictionary<string, Scope> scopes = new Dictionary<string, Scope>();

    /// <summary>
    /// Initializes a new instance of the ScopeManager class with the specified initial scope.
    /// </summary>
    /// <param name="scope">The initial scope.</param>
    public ScopeManager(Scope scope)
    {
        scopes[scope.name] = scope;
        currentScope = scope;
    }

    /// <summary>
    /// Sets the current scope to the specified scope.
    /// </summary>
    /// <param name="scope">The scope to set as the current scope.</param>
    /// <returns>True if the current scope was successfully set, false otherwise.</returns>
    public bool SetCurrentScope(Scope scope)
    {
        currentScope = scope;
        return true;
    }

    /// <summary>
    /// Creates a new scope with the specified name and parent scope.
    /// </summary>
    /// <param name="name">The name of the new scope.</param>
    /// <param name="parent">The parent scope of the new scope.</param>
    /// <param name="scope">The created Scope object, if successful.</param>
    /// <returns>True if the scope was successfully created, false if a scope with the same name already exists.</returns>
    public bool CreateScope(string name, Scope? parent, out Scope? scope)
    {
        if (scopes.ContainsKey(name))
        {
            scope = null;
            return false;
        }

        scopes[name] = new Scope(name, parent);
        scope = scopes[name];
        return true;
    }

    /// <summary>
    /// Resolves the scope with the specified name.
    /// </summary>
    /// <param name="name">The name of the scope to resolve.</param>
    /// <param name="scope">The resolved Scope object, if successful.</param>
    /// <returns>True if the scope was successfully resolved, false otherwise.</returns>
    public bool ResolveScope(string name, out Scope? scope)
    {
        return scopes.TryGetValue(name, out scope);
    }


}

public class MemoryManager
{
    private readonly Dictionary<MemoryObject, bool> ramAddresses = Enumerable.Range(0, 255).ToDictionary(key => MemoryObject.RAM(key), value => false);
    private readonly Dictionary<int, int> regValues = Enumerable.Range(0, 256).ToDictionary(key => key, value => 0);

    /// <summary>
    /// Retrieves a free RAM address from the available RAM addresses.
    /// </summary>
    /// <param name="addr">The free RAM address, if available.</param>
    /// <returns>True if a free RAM address was successfully retrieved, false otherwise.</returns>
    public bool GetFreeRamAddress(out MemoryObject? addr)
    {
        addr = ramAddresses.FirstOrDefault(kv => !kv.Value).Key;
        if (addr != null)
        {
            ramAddresses[addr] = true;
            return true;
        }
        return false;
    }
}

public class Parse
{
    public static bool Int(string input, out int value)
    {
        if (input.StartsWith("0b"))
        {
            value = Convert.ToInt32(input.Substring(2), 2);
            return true;
        } else if (input.StartsWith("0x"))
        {
            value = Convert.ToInt32(input.Substring(2), 16);
            return true;
        } else if (input.StartsWith("0d"))
        {
            return int.TryParse(input.Substring(2), out value);
        }
        else
        {
            return int.TryParse(input, out value);
            
        }
    } 
}
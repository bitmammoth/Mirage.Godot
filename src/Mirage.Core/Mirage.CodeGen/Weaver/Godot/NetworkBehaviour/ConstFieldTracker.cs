using Mirage.CodeGen;
using Mirage.Weaver;
using Mono.Cecil;

namespace Mirage.CodeGen.Weaver.Godot.NetworkBehaviour;

/// <param name="fieldName"></param>
/// <param name="type"></param>
/// <param name="max">Throws if over this count</param>
/// <param name="errorName">name of type to put in over max error</param>
internal class ConstFieldTracker(string fieldName, TypeDefinition type, int max, string errorName)
{
    private readonly string _fieldName = fieldName;
    private readonly TypeDefinition _type = type;
    private readonly int _max = max;
    private readonly string _errorName = errorName;

    public int GetInBase()
    {
        return _type.BaseType.Resolve().GetConst<int>(_fieldName);
    }

    public void Set(int countInCurrent)
    {
        var totalSyncVars = GetInBase() + countInCurrent;

        if (totalSyncVars >= _max)
            throw new NetworkBehaviourException($"{_type.Name} has too many {_errorName}. Consider refactoring your class into multiple components", _type);
        _type.SetConst(_fieldName, totalSyncVars);
    }
}

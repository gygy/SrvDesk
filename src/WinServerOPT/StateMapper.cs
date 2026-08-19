using System.Reflection;

namespace WinOpt;

internal static class StateMapper
{
    public static Dictionary<string, bool> ToMap(Optimizer.State state)
    {
        var map = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var field in typeof(Optimizer.State).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (field.FieldType == typeof(bool) && field.GetValue(state) is bool v)
                map[field.Name] = v;
        }
        return map;
    }

    public static Optimizer.State FromMap(IReadOnlyDictionary<string, bool> map)
    {
        var state = new Optimizer.State();
        foreach (var field in typeof(Optimizer.State).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (field.FieldType != typeof(bool)) continue;
            if (map.TryGetValue(field.Name, out var v))
                field.SetValue(state, v);
        }
        return state;
    }

    public static void ApplyMap(Optimizer.State target, IReadOnlyDictionary<string, bool> map)
    {
        foreach (var field in typeof(Optimizer.State).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (field.FieldType != typeof(bool)) continue;
            if (map.TryGetValue(field.Name, out var v))
                field.SetValue(target, v);
        }
    }
}

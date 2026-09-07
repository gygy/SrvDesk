using System.Reflection;

namespace SrvDesk;

internal static class StateMapper
{
    static readonly HashSet<string> SecretFields = new(StringComparer.Ordinal)
    {
        "AutologonPassword",
    };

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

    public static List<TypedSettingEntry> ToExtra(Optimizer.State state)
    {
        var list = new List<TypedSettingEntry>();
        foreach (var field in typeof(Optimizer.State).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (SecretFields.Contains(field.Name)) continue;
            if (field.FieldType == typeof(int))
            {
                list.Add(new TypedSettingEntry
                {
                    Key = field.Name,
                    Kind = "int",
                    Text = ((int)field.GetValue(state)!).ToString(),
                });
            }
            else if (field.FieldType == typeof(string))
            {
                list.Add(new TypedSettingEntry
                {
                    Key = field.Name,
                    Kind = "string",
                    Text = (string?)field.GetValue(state) ?? "",
                });
            }
        }
        return list;
    }

    public static Optimizer.State FromMap(IReadOnlyDictionary<string, bool> map)
    {
        var state = new Optimizer.State();
        ApplyMap(state, map);
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

    public static void ApplyExtra(Optimizer.State target, IEnumerable<TypedSettingEntry>? extra)
    {
        if (extra is null) return;
        var fields = typeof(Optimizer.State).GetFields(BindingFlags.Instance | BindingFlags.Public);
        foreach (var e in extra)
        {
            if (string.IsNullOrEmpty(e.Key) || SecretFields.Contains(e.Key!)) continue;
            var field = fields.FirstOrDefault(f => f.Name == e.Key);
            if (field is null) continue;
            if (string.Equals(e.Kind, "int", StringComparison.OrdinalIgnoreCase) && field.FieldType == typeof(int)
                && int.TryParse(e.Text, out var iv))
                field.SetValue(target, iv);
            else if (string.Equals(e.Kind, "string", StringComparison.OrdinalIgnoreCase) && field.FieldType == typeof(string))
                field.SetValue(target, e.Text ?? "");
        }
    }
}

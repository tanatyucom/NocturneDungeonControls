using System;
using System.Linq;
using System.Reflection;
using Il2Cpp;

internal static class Program
{
    private const BindingFlags AllStatic =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    private static void Main()
    {
        Console.WriteLine("=== SIActionName camera/reset actions ===");
        foreach (string name in Enum.GetNames(typeof(SIActionName))
                     .Where(name => name.Contains("Camera", StringComparison.OrdinalIgnoreCase) ||
                                    name.Contains("Front", StringComparison.OrdinalIgnoreCase) ||
                                    name.Contains("Reset", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(name);
        }

        Dump(typeof(cmpMisc), "cmpUseSkillTokusyu");
        Dump(typeof(cmpMisc), "cmpUseItemTokusyu");
        Dump(typeof(cmpCalc), "cmpStartSkillMsg");
        Dump(typeof(datCalc), "datExecSkill");

        Console.WriteLine("\n=== dds3GlobalWork.DDS3_GBWK ===");
        MemberInfo[] globals = typeof(dds3GlobalWork).GetMember("DDS3_GBWK", AllStatic);
        foreach (MemberInfo member in globals)
        {
            Console.WriteLine(DescribeMember(member));
            Type? globalType = member is FieldInfo field
                ? field.FieldType
                : member is PropertyInfo property ? property.PropertyType : null;
            if (globalType == null)
            {
                continue;
            }

            Console.WriteLine($"--- {globalType.FullName}.unitwork ---");
            foreach (MemberInfo unitwork in globalType.GetMember(
                         "unitwork",
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.Static))
            {
                Console.WriteLine(DescribeMember(unitwork));
            }
        }

        DumpMembersByTerms(
            typeof(Il2Cppnewdata_H.datUnitWork_t),
            "hp", "mp", "skill", "party", "stock", "dead", "stat", "summon");
        DumpMethodsByTerms(
            typeof(cmpMisc),
            "UseSkill", "Skill", "Heal", "Recover", "Cure", "Camp");
        DumpMethodsByTerms(
            typeof(cmpCalc),
            "UseSkill", "Skill", "Heal", "Recover", "Cure", "Camp", "Stock", "Party");
        DumpMembersByTerms(
            typeof(Il2Cppdds3GlobalWork_H.dds3GlobalWork_t),
            "party", "stock", "unit", "summon", "entry", "formation", "member");
        DumpAllProperties(typeof(Il2Cppnewdata_H.datUnitWork_t));
        DumpAssemblySkillApis();
        DumpAssemblyPartyMembers();
        DumpMembersByTerms(
            typeof(fldCamera),
            "Move", "Dir", "Rot", "Camera", "Cam", "Main", "Normal");
    }

    private static void Dump(Type type, string methodName)
    {
        Console.WriteLine($"\n=== {type.FullName}.{methodName} ===");
        MethodInfo[] methods = type.GetMethods(AllStatic)
            .Where(method => method.Name == methodName)
            .ToArray();
        if (methods.Length == 0)
        {
            Console.WriteLine("METHOD NOT FOUND");
        }
        foreach (MethodInfo method in methods)
        {
            string access = method.IsPublic ? "public" : method.IsPrivate ? "private" : "non-public";
            string parameters = string.Join(", ", method.GetParameters()
                .Select(parameter => $"{parameter.ParameterType.FullName} {parameter.Name}"));
            Console.WriteLine($"{access} static {method.ReturnType.FullName} {method.Name}({parameters})");
        }

        string needle = "NativeMethodInfoPtr_" + methodName + "_";
        FieldInfo[] fields = type.GetFields(AllStatic)
            .Where(field => field.Name.StartsWith(needle, StringComparison.Ordinal))
            .ToArray();
        if (fields.Length == 0)
        {
            Console.WriteLine("NATIVE FIELD NOT FOUND");
        }
        foreach (FieldInfo field in fields)
        {
            Console.WriteLine($"native-field {field.Attributes}: {field.FieldType.FullName} {field.Name}");
        }
    }

    private static string DescribeMember(MemberInfo member)
    {
        return member switch
        {
            FieldInfo field => $"field {field.Attributes}: {field.FieldType.FullName} {field.Name}",
            PropertyInfo property => $"property {property.PropertyType.FullName} {property.Name}",
            _ => member.ToString() ?? member.Name
        };
    }

    private static void DumpMembersByTerms(Type type, params string[] terms)
    {
        Console.WriteLine($"\n=== {type.FullName} relevant members ===");
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                             BindingFlags.Instance | BindingFlags.Static;
        foreach (MemberInfo member in type.GetMembers(flags)
                     .Where(member => terms.Any(term =>
                         member.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
                     .OrderBy(member => member.Name))
        {
            Console.WriteLine(DescribeMember(member));
        }
    }

    private static void DumpMethodsByTerms(Type type, params string[] terms)
    {
        Console.WriteLine($"\n=== {type.FullName} relevant methods ===");
        foreach (MethodInfo method in type.GetMethods(AllStatic)
                     .Where(method => terms.Any(term =>
                         method.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
                     .OrderBy(method => method.Name))
        {
            string access = method.IsPublic ? "public" : method.IsPrivate ? "private" : "non-public";
            string parameters = string.Join(", ", method.GetParameters()
                .Select(parameter => $"{parameter.ParameterType.FullName} {parameter.Name}"));
            Console.WriteLine($"{access} static {method.ReturnType.FullName} {method.Name}({parameters})");
        }
    }

    private static void DumpAllProperties(Type type)
    {
        Console.WriteLine($"\n=== {type.FullName} all properties ===");
        foreach (PropertyInfo property in type.GetProperties(
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.Static)
                     .OrderBy(property => property.Name))
        {
            Console.WriteLine($"property {property.PropertyType.FullName} {property.Name}");
        }
    }

    private static void DumpAssemblySkillApis()
    {
        Console.WriteLine("\n=== Assembly skill/recovery data APIs ===");
        string[] terms =
        {
            "GetSkill", "SkillCost", "SkillData", "SkillTable",
            "Recover", "Recovery", "BadStatus", "Cure"
        };
        foreach (Type type in typeof(cmpMisc).Assembly.GetTypes()
                     .OrderBy(type => type.FullName))
        {
            MethodInfo[] methods;
            try
            {
                methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Static | BindingFlags.Instance)
                    .Where(method => terms.Any(term =>
                        method.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
            }
            catch
            {
                continue;
            }
            foreach (MethodInfo method in methods)
            {
                string parameters = string.Join(", ", method.GetParameters()
                    .Select(parameter => $"{parameter.ParameterType.Name} {parameter.Name}"));
                Console.WriteLine($"{type.FullName}.{method.Name}({parameters}) -> {method.ReturnType.Name}");
            }
        }
    }

    private static void DumpAssemblyPartyMembers()
    {
        Console.WriteLine("\n=== Assembly party/formation members ===");
        string[] terms =
        {
            "Party", "Formation", "Summon", "StockList", "BattleUnit"
        };
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                             BindingFlags.Static | BindingFlags.Instance |
                             BindingFlags.DeclaredOnly;
        foreach (Type type in typeof(cmpMisc).Assembly.GetTypes()
                     .OrderBy(type => type.FullName))
        {
            MemberInfo[] members;
            try
            {
                members = type.GetMembers(flags)
                    .Where(member => terms.Any(term =>
                        member.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
            }
            catch
            {
                continue;
            }

            foreach (MemberInfo member in members)
            {
                if (member is MethodInfo method && method.IsSpecialName)
                {
                    continue;
                }
                Console.WriteLine($"{type.FullName}: {DescribeMember(member)}");
            }
        }
    }
}

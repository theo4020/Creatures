using System.IO;
using System.Text;
using UnityEngine;
using System.Globalization;

public class CreatureSaver
{
    public static void Save(Creature creature, string fileName = "creature")
    {
        var sb = new StringBuilder();
        
        WriteLimb(sb, creature.body, 0);
        
        string folder = Path.Combine(Application.dataPath, "SavedCreatures");

        // Crée le dossier s'il n'existe pas
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string path = Path.Combine(folder, fileName + ".txt");
        
        File.WriteAllText(path, sb.ToString());
        Debug.Log("Creature saved to: " + path);
    }

    private static void WriteLimb(StringBuilder sb, Limb limb, int depth)
    {
        string indent = new string('-', depth);
        var c = CultureInfo.InvariantCulture;
    
        sb.AppendLine($"{indent}[LIMB]");
        sb.AppendLine($"{indent}isBody={limb.data.isBody}");
        sb.AppendLine($"{indent}position={V3(limb.transform.position)}");
        sb.AppendLine($"{indent}rotation={Q(limb.data.rotation)}");
        sb.AppendLine($"{indent}scale={limb.data.scale.ToString(c)}");
        sb.AppendLine($"{indent}radius={limb.data.radius.ToString(c)}");
        sb.AppendLine($"{indent}height={limb.data.height.ToString(c)}");
        sb.AppendLine($"{indent}isTopTaken={limb.data.isTopTaken}");
        sb.AppendLine($"{indent}isTopAttached={limb.data.isTopAttached}");
        sb.AppendLine($"{indent}nbAttachedLimb={limb.nbAttachedLimb}");
        sb.AppendLine($"{indent}[/LIMB]");

        foreach (var child in limb.limbs)
            WriteLimb(sb, child, depth + 1);
    }
    
    private static string V3(Vector3 v)
    {
        var c = CultureInfo.InvariantCulture;
        return $"{v.x.ToString(c)},{v.y.ToString(c)},{v.z.ToString(c)}";
    }

    private static string Q(Quaternion q)
    {
        var c = CultureInfo.InvariantCulture;
        return $"{q.x.ToString(c)},{q.y.ToString(c)},{q.z.ToString(c)},{q.w.ToString(c)}";
    }
}
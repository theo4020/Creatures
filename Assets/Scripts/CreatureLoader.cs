using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class CreatureLoader
{
    public static Creature Load(string fileName, GameObject creatureGo)
    {
        string folder = Path.Combine(Application.dataPath, "SavedCreatures");

        // Crée le dossier s'il n'existe pas
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string path = Path.Combine(folder, fileName + ".txt");
        
        
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return null;
        }

        string[] lines = File.ReadAllLines(path);
        Creature creature = creatureGo.AddComponent<Creature>();
        
        int index = 0;
        creature.body = ReadLimb(lines, ref index, null, 0);
        
        return creature;
    }

    private static Limb ReadLimb(string[] lines, ref int index, Limb parent, int depth)
    {
        while (index < lines.Length && string.IsNullOrWhiteSpace(lines[index]))
            index++;

        if (index >= lines.Length) return null;

        // Vérifie que le niveau de profondeur correspond
        string raw = lines[index];
        int lineDepth = CountDashes(raw);
        if (lineDepth != depth) return null;

        string firstLine = raw.TrimStart('-');
        if (firstLine != "[LIMB]") return null;
        index++;

        bool isBody = false;
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        float scale = 1f;
        float radius = 0.5f;
        float height = 2f;
        bool isTopTaken = false;
        bool isTopAttached = false;
        int nbAttached = 0;

        while (index < lines.Length)
        {
            string line = lines[index].TrimStart('-');

            if (line == "[/LIMB]") { index++; break; }

            var parts = line.Split('=');
            if (parts.Length < 2) { index++; continue; }

            string key   = parts[0];
            string value = parts[1];

            switch (key)
            {
                case "isBody":         isBody     = bool.Parse(value);   break;
                case "position":       position   = ParseV3(value);      break;
                case "rotation":       rotation   = ParseQ(value);       break;
                case "scale":          scale      = ParseFloat(value);   break;
                case "radius":         radius     = ParseFloat(value);   break;
                case "height":         height     = ParseFloat(value);   break;
                case "isTopTaken":     isTopTaken = bool.Parse(value);   break;
                case "isTopAttached":  isTopAttached = bool.Parse(value);   break;
                case "nbAttachedLimb": nbAttached = int.Parse(value);    break;
            }

            index++;
        }

        var data = new LimbData(isBody, position, rotation, scale, radius, height, isTopTaken, isTopAttached);

        Limb limb = Limb.Create(data, parent, isRandom: false);
        limb.nbAttachedLimb = nbAttached;

        // Lit les enfants au niveau depth+1
        while (index < lines.Length)
        {
            while (index < lines.Length && string.IsNullOrWhiteSpace(lines[index]))
                index++;

            if (index >= lines.Length) break;

            int childDepth = CountDashes(lines[index]);
            if (childDepth != depth + 1) break; // pas un enfant direct

            Limb child = ReadLimb(lines, ref index, limb, depth + 1);
            if (child != null)
                limb.limbs.Add(child);
        }

        return limb;
    }

    private static int CountDashes(string line)
    {
        int count = 0;
        foreach (char c in line)
        {
            if (c == '-') count++;
            else break;
        }
        return count;
    }

    private static Vector3 ParseV3(string s)
    {
        var c = CultureInfo.InvariantCulture;
        var p = s.Split(',');
        return new Vector3(float.Parse(p[0], c), float.Parse(p[1], c), float.Parse(p[2], c));
    }

    private static Quaternion ParseQ(string s)
    {
        var c = CultureInfo.InvariantCulture;
        var p = s.Split(',');
        return new Quaternion(float.Parse(p[0], c), float.Parse(p[1], c), float.Parse(p[2], c), float.Parse(p[3], c));
    }

    private static float ParseFloat(string s)
    {
        return float.Parse(s, CultureInfo.InvariantCulture);
    }
}
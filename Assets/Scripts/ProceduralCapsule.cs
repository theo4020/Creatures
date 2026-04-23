using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(CapsuleCollider))]
public class ProceduralCapsule : MonoBehaviour
{
    [Header("Dimensions")]
    public float radius = 0.5f;
    public float height = 2f;
    public int segments = 12;

    void Start() => Apply();

    // Appelé automatiquement quand tu changes une valeur dans l'Inspector
    void OnValidate()
    {
        // Sécurité : évite les valeurs invalides
        radius  = Mathf.Max(0.01f, radius);
        height  = Mathf.Max(radius * 2f, height); // height ne peut pas être < 2*radius
        segments = Mathf.Max(6, segments);

        // Nécessaire car OnValidate peut être appelé avant Start
        if (GetComponent<MeshFilter>() != null)
            Apply();
    }

    public void Apply()
    {
        GetComponent<MeshFilter>().mesh = CreateCapsuleMesh(radius, height, segments);

        var col = GetComponent<CapsuleCollider>();
        col.radius    = radius;
        col.height    = height;
        col.direction = 1;      // axe Y
        col.center    = Vector3.zero;
    }

    Mesh CreateCapsuleMesh(float radius, float height, int segments)
    {
        float cylinderHeight = Mathf.Max(0, height - 2 * radius);
        int rings = segments / 2;
        int cols  = segments + 1;

        var verts = new System.Collections.Generic.List<Vector3>();
        var tris  = new System.Collections.Generic.List<int>();

        for (int r = 0; r <= rings; r++)
        {
            float phi = Mathf.PI * 0.5f * (1f - (float)r / rings);
            float y   =  cylinderHeight * 0.5f + radius * Mathf.Sin(phi);
            float xzR = radius * Mathf.Cos(phi);
            for (int s = 0; s <= segments; s++)
            {
                float theta = 2f * Mathf.PI * s / segments;
                verts.Add(new Vector3(xzR * Mathf.Cos(theta), y, xzR * Mathf.Sin(theta)));
            }
        }

        int basStart = (cylinderHeight > 0.001f) ? 0 : 1;
        for (int r = basStart; r <= rings; r++)
        {
            float phi = Mathf.PI * 0.5f * (float)r / rings;
            float y   = -cylinderHeight * 0.5f - radius * Mathf.Sin(phi);
            float xzR = radius * Mathf.Cos(phi);
            for (int s = 0; s <= segments; s++)
            {
                float theta = 2f * Mathf.PI * s / segments;
                verts.Add(new Vector3(xzR * Mathf.Cos(theta), y, xzR * Mathf.Sin(theta)));
            }
        }

        int totalRows = verts.Count / cols - 1;
        for (int r = 0; r < totalRows; r++)
        {
            for (int s = 0; s < segments; s++)
            {
                int a = r * cols + s;
                int b = r * cols + s + 1;
                int c = (r + 1) * cols + s;
                int d = (r + 1) * cols + s + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }

        var mesh = new Mesh();
        mesh.vertices  = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
}
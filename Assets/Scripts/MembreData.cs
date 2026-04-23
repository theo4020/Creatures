using UnityEngine;

[CreateAssetMenu(fileName = "Membre", menuName = "Scriptable Objects/Membre")]
public class MembreData : ScriptableObject
{
    public Transform transform;
    public float radius;
    public float height;
    public bool isBody;
    
    public MembreData(bool isBody, Transform transform, float radius, float height)
    {
        Init(isBody, transform, radius, height);
    }

    public MembreData(bool isBody)
    {
        this.isBody = isBody;
        Init(isBody, Vector3.zero);
    }
    
    public MembreData(bool isBody, Vector3 position)
    {
        Init(isBody, position);
    }
    
    private void Init(bool isBody, Transform transform, float radius, float height)
    {
        this.isBody = isBody;
        this.transform = transform;
        this.radius = radius;
        this.height = height;
    }
    
    private void Init(bool isBody, Vector3 position)
    {
        this.isBody = isBody;
        if (this.isBody)
        {
            transform.position = Vector3.zero;
        }
        else
        {
            transform.position = position;
        }
        transform.rotation = Random.rotation;
        float randomScale = Random.Range(0.5f, 10f);
        transform.localScale = new Vector3(randomScale, randomScale, randomScale);
        radius = Random.Range(0.5f, 5f);
        height = Random.Range(radius*2, 10f);
    }
}

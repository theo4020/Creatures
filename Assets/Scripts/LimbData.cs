using UnityEngine;

[System.Serializable] // permet de voir les champs dans l'Inspector si besoin
public class LimbData
{
    public Vector3 position;
    public Quaternion rotation;
    public float scale;
    public float radius;
    public float height;
    public bool isBody;
    public bool isTopTaken;
    
    public LimbData(bool isBody, Vector3 position, Quaternion rotation, float scale, float radius, float height)
    {
        Init(isBody, position, rotation, scale, radius, height);
    }

    public LimbData(bool isBody)
    {
        this.isBody = isBody;
        if (this.isBody)
        {
            Init(isBody, Vector3.zero);
        }
        else
        {
            Init(isBody, Random.insideUnitSphere*5);
        }
    }
    
    public LimbData(bool isBody, Vector3 position)
    {
        Init(isBody, position);
    }
    
    private void Init(bool isBody, Vector3 position, Quaternion rotation, float scale, float radius, float height)
    {
        this.isBody = isBody;
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;
        this.radius = radius;
        this.height = height;
    }
    
    private void Init(bool isBody, Vector3 position)
    {
        this.isBody = isBody;
        if (this.isBody)
        {
            this.position = Vector3.zero;
        }
        else
        {
            this.position = position;
        }
        rotation = Random.rotation;
        scale = Random.Range(0.5f, 2f);
        if (this.isBody) scale *= 2f;
        radius = Random.Range(0.5f, 2f);
        height = Random.Range(radius*2, 5f);
    }
}

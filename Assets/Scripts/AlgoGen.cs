using System.Collections.Generic;
using UnityEngine;

public class AlgoGen : MonoBehaviour
{
    public MembreData body;
    public List<MembreData> membresLvl1 = new List<MembreData>();
    public List<List<MembreData>> membresLvl2 = new List<List<MembreData>>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CreateFromStart()
    {
        MembreData body = new MembreData(true);
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DotSpawner : MonoBehaviour
{

    public GameObject dot;
    private float timeS;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time > timeS + 0.2f)
        {
            timeS = Time.time;

            GameObject newDot = Instantiate(dot);
            newDot.transform.position = this.transform.position;
        }

        
    }
}

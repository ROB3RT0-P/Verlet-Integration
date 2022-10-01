using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSphere : MonoBehaviour
{
    private Vector3 dir = Vector3.forward;
    public float speed = 5.0f;

    // Start is called before the first frame update
    void Start(){
        transform.position = new Vector3(10, 8, 0);
    }

    // Update is called once per frame
    void Update(){
        transform.Translate(dir*speed*Time.deltaTime);
 
        if(transform.position.z <= -15){
            dir = Vector3.forward;
        }else if(transform.position.z >= 15){
            dir = Vector3.back;
      }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RaycasterScript : MonoBehaviour
{

    private LineRenderer _lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        
    }

    // Update is called once per frame
    void Update()
    {

        RaycastHit hit;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Default"))){
            _lineRenderer.SetPositions(new Vector3[2]{
                Camera.main.transform.position,
                hit.point
            });
        }else{
            _lineRenderer.SetPositions(new Vector3[2]{
                Vector3.zero,
                Vector3.zero
            });
        }
        
    }
}

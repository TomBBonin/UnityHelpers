/* 
 * Simple component to drag objects at run time in 3D. 
 * I haven't looked too much into this, so the Z position when dragging is probably off.
 * To be revisited. For now it serves its purpose.
 * If you use and improve it, let me know, i'd be happy to have a betetr version.
 * 
 * To use : 
 * DraggingSphere dragSphere = ObjIWantToDrag.AddComponent<DraggingSphere>();
 * dragSphere.Init(Radius);
 * 
 * /!\ This uses a Transparent Color shader that you can find on my repo under /Shaders
 * 
 * https://github.com/TomBBonin/UnityHelpers
 */

using UnityEngine;
using System.Collections;

public class DraggingSphere : MonoBehaviour
{
    public bool             Dragging;

    private SphereCollider  _collider;
    private MeshRenderer    _renderer;
    private GameObject      _view;
    private int             _radius;
    private float           _cameraDist;

    public void Init(int radius)
    {
        _view = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _view.transform.parent = transform;
        _view.transform.localPosition = Vector3.zero;
        Destroy(_view.GetComponent<SphereCollider>());
        _renderer = _view.GetComponent<MeshRenderer>();
        _collider = gameObject.AddComponent<SphereCollider>();

        _radius = radius;
        _cameraDist = Vector3.Distance(transform.position, Camera.main.transform.position);
        Shader transparentShader = Shader.Find("Custom/TransparentColor");
        if (transparentShader != null)
            _renderer.material.shader = transparentShader;
        Dragging = false;
        UpdateView();
    }

    public void UpdateView(int radius = -1)
    {
        // You can change the Radius at run time if needed
        _radius = (radius != -1) ? radius : _radius;
        _view.transform.localScale = new Vector3(1f, 1f, 1f) * _radius * 4f;
        _collider.radius = _radius * 2f;
        Color color = Dragging ? Color.green : Color.gray;
        color.a = 0.2f;
        _renderer.material.color = color; 
    }

    public void OnMouseOver()
    {
        if (Input.GetMouseButton(0)) // on left click start dragging
        {
            if (!Dragging)
            {
                Dragging = true;
                UpdateView();
            }
            transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _cameraDist));
        }
        else if (Dragging)
        {
            Dragging = false;
            UpdateView();
        }
    }
}
using System;
using QFSW.QC;
using Sensoren;
using UnityEngine;

public class ArrowNavigatorScript : MonoBehaviour
{
    private Transform _target;

    [SerializeField] private Transform _golfball;

    private bool _golfballGesetzt;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _target = GameObject.FindAnyObjectByType<GolfHoleSensor>().transform;
        if (_target != null)
            Debug.Log("Found no Golf hole to point to");
        if (_golfball != null)
            _golfballGesetzt = true;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(_target.position);
        if (_golfballGesetzt)
            transform.position = new Vector3(_golfball.position.x, _golfball.position.y + 0.1f, _golfball.position.z);
    }
    
    public void SetParent(Transform parent)
    {
        transform.parent = parent;
        foreach (Transform child in parent)
        {
            if (child.CompareTag("GolfBall"))
                _golfball = child;
        }

        _golfballGesetzt = true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class VarInstance : MonoBehaviour {

    public static VarInstance instance;

    private void Awake()
    {
        instance = this;
    }

    [Range(-2f, 2f)]
    public float scale = 0.5f;
    [Range(-2f, 2f)]
    public float translate = 0.5f;
}

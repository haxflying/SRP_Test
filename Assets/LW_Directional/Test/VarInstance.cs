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

    [Range(0, 10)]
    public float mipLevel = 0;

    public bool useCustomMatrix = false;
}

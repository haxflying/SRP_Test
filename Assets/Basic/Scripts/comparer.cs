using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


public class LightComparer : IComparer<VisibleLight>
{
    public Vector3 camPos;

    public int Compare(VisibleLight x, VisibleLight y)
    {
        //put baked light back 
        if (x.light.bakingOutput.isBaked && !y.light.bakingOutput.isBaked)
            return 1;

        if (x.lightType == LightType.Directional && y.lightType != LightType.Directional)
            return -1;
        else if (x.lightType != LightType.Directional && y.lightType == LightType.Directional)
            return 1;

        return Mathf.Abs((x.light.transform.position - camPos).sqrMagnitude).CompareTo(
            Mathf.Abs((y.light.transform.position - camPos).sqrMagnitude));
    }

}

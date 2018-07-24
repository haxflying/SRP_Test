using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shadowCaster : MonoBehaviour {

    public static List<shadowCaster> casters = new List<shadowCaster>(pipeline_basic_asset.MAX_SHADOWCASTERS);

    static void CalcuShadowMatrices(Light l, Renderer r, out Matrix4x4 view, out Matrix4x4 proj, out float distance)
    {
        float aspect = 1f;
        float nearClip = 0.01f;
        float farClip;
        float fov;

        Vector3 pos = Vector3.zero;
        Vector3 forw = Vector3.forward;
        Quaternion rot = Quaternion.identity;
        Matrix4x4 projMartix = Matrix4x4.identity;

        bool ortho = l.type == LightType.Area || l.type == LightType.Directional;

        if(ortho)
        {
            forw = l.transform.forward;
            rot = l.transform.rotation;
            rot = Quaternion.LookRotation(forw, Vector3.up);

            pos = l.type == LightType.Area ?
                l.transform.position :
                r.transform.position - (forw * r.bounds.extents.magnitude);
        }
        else
        {
            //TODO
        }

        Matrix4x4 worldToShadow = Matrix4x4.TRS(pos, rot, Vector3.one).inverse;

        Vector3 boundsMin = r.bounds.min;
        Vector3 boundsMax = r.bounds.max;

        Vector3 fbl, fbr, ftl, ftr, bbl, bbr, btl, btr;
        fbl = boundsMin;
        fbr = new Vector3(boundsMax.x, boundsMin.y, boundsMin.z);
        ftl = new Vector3(boundsMin.x, boundsMax.y, boundsMin.z);
        ftr = new Vector3(boundsMax.x, boundsMax.y, boundsMin.z);
        bbl = new Vector3(boundsMin.x, boundsMin.y, boundsMax.z);
        bbr = new Vector3(boundsMax.x, boundsMin.y, boundsMax.z);
        btl = new Vector3(boundsMin.x, boundsMax.y, boundsMax.z);
        btr = boundsMax;

        fbl = worldToShadow.MultiplyPoint3x4(fbl);
        fbr = worldToShadow.MultiplyPoint3x4(fbr);
        ftl = worldToShadow.MultiplyPoint3x4(ftl);
        ftr = worldToShadow.MultiplyPoint3x4(ftr);
        bbl = worldToShadow.MultiplyPoint3x4(bbl);
        bbr = worldToShadow.MultiplyPoint3x4(bbr);
        btl = worldToShadow.MultiplyPoint3x4(btl);
        btr = worldToShadow.MultiplyPoint3x4(btr);

        float minX = Mathf.Min(fbl.x, Mathf.Min(fbr.x, Mathf.Min(ftl.x, Mathf.Min(ftr.x, Mathf.Min(bbl.x, Mathf.Min(bbr.x, Mathf.Min(btl.x, btr.x)))))));
        float maxX = Mathf.Max(fbl.x, Mathf.Max(fbr.x, Mathf.Max(ftl.x, Mathf.Max(ftr.x, Mathf.Max(bbl.x, Mathf.Max(bbr.x, Mathf.Max(btl.x, btr.x)))))));
        float minY = Mathf.Min(fbl.y, Mathf.Min(fbr.y, Mathf.Min(ftl.y, Mathf.Min(ftr.y, Mathf.Min(bbl.y, Mathf.Min(bbr.y, Mathf.Min(btl.y, btr.y)))))));
        float maxY = Mathf.Max(fbl.y, Mathf.Max(fbr.y, Mathf.Max(ftl.y, Mathf.Max(ftr.y, Mathf.Max(bbl.y, Mathf.Max(bbr.y, Mathf.Max(btl.y, btr.y)))))));
        float minZ = Mathf.Min(fbl.z, Mathf.Min(fbr.z, Mathf.Min(ftl.z, Mathf.Min(ftr.z, Mathf.Min(bbl.z, Mathf.Min(bbr.z, Mathf.Min(btl.z, btr.z)))))));
        float maxZ = Mathf.Max(fbl.z, Mathf.Max(fbr.z, Mathf.Max(ftl.z, Mathf.Max(ftr.z, Mathf.Max(bbl.z, Mathf.Max(bbr.z, Mathf.Max(btl.z, btr.z)))))));

        Vector3 min = new Vector3(minX, minY, minZ);
        Vector3 max = new Vector3(maxX, maxY, maxZ);

        farClip = l.type == LightType.Directional ? maxZ : l.range;

        if(ortho)
        {
            float size = 0.5f * (maxY - minY);
            projMartix = Matrix4x4.Ortho(
                -aspect * size, aspect * size,
                -size, size, nearClip, farClip);
        }
        else
        {
            //TODO
        }

        //inverse Z
        worldToShadow.SetRow(2, -worldToShadow.GetRow(2));

        view = worldToShadow;
        proj = projMartix;
        distance = 1f / farClip;
    }
}

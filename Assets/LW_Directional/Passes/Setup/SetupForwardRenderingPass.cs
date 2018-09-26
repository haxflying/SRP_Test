using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MZ.LWD;
using UnityEngine.Experimental.Rendering;

class SetupForwardRenderingPass : ScriptableRenderPass
{
    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        context.SetupCameraProperties(renderingData.cameraData.camera, false);
    }
}


using MZ.LWD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public interface IRendererSetup
{
    void Setup(ScriptableRenderer renderer, ref RenderingData renderingData);
}


using Kogl.Common;
using Kogl.Samples.Samples;

namespace Kogl.Samples;

internal class Program
{
    private static void Main()
    {
        Log.Info("KoGL - Samples");

        TextureLoadingExample.Start();
        // ShadowMappingExample.Start();
        RaymarchedPrimitivesExample.Start();
        RaymarchedHillsExample.Start();
        SpriteRenderingExample.Start();
        MultiTextureMaterialExample.Start();
        SimpleExample.Start();
        MaterialExample.Start();
        InputExample.Start();
        TextRenderingExample.Start();
        CameraExample.Start();
        Cube3DExample.Start();
        ScissorExample.Start();
        CustomShadersExample.Start();
        PostProcessingExample.Start();
    }
}

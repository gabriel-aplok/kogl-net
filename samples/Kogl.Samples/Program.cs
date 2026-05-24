using Kogl.Common;
using Kogl.Samples.Samples;

namespace Kogl.Samples;

public class ExampleInfo(string name, Action main)
{
    public string Name { get; set; } = name;
    public Action StartMethod { get; set; } = main;
}

public class ExampleList
{
    private static readonly ExampleInfo[] _allExamples =
    [
        new ExampleInfo("Camera", CameraExample.Start),
        new ExampleInfo("BasicLighting", BasicLightingExample.Start),
        // new ExampleInfo("Ui", UiExample.Start),
        // new ExampleInfo("TextRendering", TextRenderingExample.Start),
        // new ExampleInfo("JitterPhysicsDrop", JitterPhysicsDropExample.Start),
        // new ExampleInfo("JitterCarBridge", JitterCarBridgeExample.Start),
        // new ExampleInfo("MultiTextureMaterial", MultiTextureMaterialExample.Start),
        // new ExampleInfo("ObjLoaderTest", ObjLoaderTestExample.Start),
        // new ExampleInfo("CustomShaders", CustomShadersExample.Start),
        // new ExampleInfo("CustomShaders2", CustomShaders2Example.Start),
        // new ExampleInfo("AssetManager", AssetManagerExample.Start),
        // new ExampleInfo("TextureLoading", TextureLoadingExample.Start),
        // new ExampleInfo("RaymarchedHills", RaymarchedHillsExample.Start),
        // new ExampleInfo("RaymarchedPrimitives", RaymarchedPrimitivesExample.Start),
        // new ExampleInfo("SpriteRendering", SpriteRenderingExample.Start),
        // new ExampleInfo("Simple", SimpleExample.Start),
        // new ExampleInfo("Material", MaterialExample.Start),
        // new ExampleInfo("Input", InputExample.Start),
        // new ExampleInfo("Gizmo", SampleGizmo.Start),
        // new ExampleInfo("Cube3D", Cube3DExample.Start),
        // new ExampleInfo("Scissor", ScissorExample.Start),
        // new ExampleInfo("PostProcessing", PostProcessingExample.Start),
    ];

    public static ExampleInfo[] GetAllExamples()
    {
        return _allExamples;
    }

    public static ExampleInfo? GetExample(string name)
    {
        return Array.Find(
            GetAllExamples(),
            x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
        );
    }
}

internal class Program
{
    private static void Main(string[] args)
    {
        LogCat.Info("Kolpa - Samples");

        if (args.Length > 0)
        {
            ExampleInfo? example = ExampleList.GetExample(args[0]);
            example?.StartMethod?.Invoke();
            return;
        }

        foreach (ExampleInfo example in ExampleList.GetAllExamples())
        {
            example.StartMethod?.Invoke();
        }
    }
}

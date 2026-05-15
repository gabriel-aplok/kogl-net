using Kogl.Samples.Samples;

namespace Kogl.Samples;

internal class Program
{
    private static void Main()
    {
        Console.WriteLine("KoGL - Samples");
        Console.WriteLine("==============");
        Console.WriteLine();

        SimpleExample.Start();
        Cube3DExample.Start();
        TextureLoadingExample.Start();
        ScissorExample.Start();
        CustomShadersExample.Start();
        PostProcessingExample.Start();
    }
}

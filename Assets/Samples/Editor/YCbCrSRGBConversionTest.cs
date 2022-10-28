using UnityEngine;
using NUnit.Framework;
using Unity.Mathematics;

public sealed class YCbCrSRGBConversionTest
{
    static readonly float4x4 s_YCbCrToSRGB = new float4x4(
        1.0f,  0.0000f,  1.4020f, -0.7010f,
        1.0f, -0.3441f, -0.7141f,  0.5291f,
        1.0f,  1.7720f,  0.0000f, -0.8860f,
        0.0f,  0.0000f,  0.0000f,  1.0000f
    );

    [TestCase(0.5f, 0.5f, 0.5f)]
    public void YCbCrToSRGBTest(float y, float cb, float cr)
    {
        // Get inverse  s_YCbCrToSRGB
        var s_YCbCrToSRGBInv = math.inverse(s_YCbCrToSRGB);
        Debug.Log(s_YCbCrToSRGBInv);

        // Debug.Log($"s_YCbCrToSRGB: {s_YCbCrToSRGB} transposed: {math.transpose(s_YCbCrToSRGB)}");
        
        // var ycbcr = new float3(y, cb, cr);
        // var srgb1 = YCbCrToSRGB(y, new float2(cb, cr));
        // var srgb2 = math.mul(s_YCbCrToSRGB, ycbcr);

        // Debug.Log($"ycbcr={ycbcr} -> srgb1={srgb1}, srng2={srgb2}");
        // Assert.AreEqual(srgb1, srgb2);
        // Assert.AreEqual(new float3(0.5f, 0.5f, 0.5f), srgb1);
    }


}

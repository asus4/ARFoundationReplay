using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.ARSubsystems;

public class PlaneSubsystemTest
{
    // A Test behaves as an ordinary method
    [Test]
    public void SerializedTypeValidation()
    {
        Assert.IsTrue(UnsafeUtility.IsBlittable<BoundedPlane>());
    }

}

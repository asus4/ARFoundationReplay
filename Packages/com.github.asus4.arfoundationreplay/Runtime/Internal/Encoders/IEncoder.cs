using UnityEngine;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    /// <summary>
    /// Base interface for all encoders.
    /// </summary>
    internal interface IEncoder : System.IDisposable
    {
        bool Initialize(XROrigin origin, Packet packet, Material material);
        void Update();
    }
}

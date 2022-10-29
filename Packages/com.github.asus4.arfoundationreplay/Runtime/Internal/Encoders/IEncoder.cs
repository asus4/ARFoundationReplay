using UnityEngine;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    internal interface IEncoder : System.IDisposable
    {
        bool Initialize(XROrigin origin, Packet packet, Material material);
        void Update();
    }
}

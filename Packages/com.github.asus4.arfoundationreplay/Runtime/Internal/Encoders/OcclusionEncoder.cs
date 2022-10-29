using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{

    internal sealed class OcclusionEncoder : IEncoder
    {
        private Packet _packet;
        private Material _material;
        private AROcclusionManager _occlusionManager;


        public bool Initialize(XROrigin origin, Packet packet, Material material)
        {
            _packet = packet;
            _material = material;
            _occlusionManager = origin.GetComponentInChildren<AROcclusionManager>();
            if (_occlusionManager == null)
            {
                return false;
            }
            _occlusionManager.frameReceived += OnOcclusionFrameReceived;
            return true;
        }

        public void Dispose()
        {
            if (_occlusionManager != null)
            {
                _occlusionManager.frameReceived -= OnOcclusionFrameReceived;
            }
            _occlusionManager = null;
            _packet = null;
            _material = null;
        }

        public void Update()
        {
            // Nothing to do
        }

        private void OnOcclusionFrameReceived(AROcclusionFrameEventArgs args)
        {
            // Set texture
            var count = args.textures.Count;
            for (int i = 0; i < count; i++)
            {
                _material.SetTexture(args.propertyNameIds[i], args.textures[i]);
            }
        }
    }
}

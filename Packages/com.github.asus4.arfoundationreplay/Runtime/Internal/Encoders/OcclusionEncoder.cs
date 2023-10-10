using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    internal sealed class OcclusionEncoder : ISubsystemEncoder
    {
        private static readonly int k_DepthRange = Shader.PropertyToID("_DepthRange");
        private Material _muxMaterial;
        private AROcclusionManager _occlusionManager;
        private readonly Vector2 _depthRange = Config.DepthRange;

        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            _muxMaterial = muxMaterial;
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
            _muxMaterial = null;
        }

        public void Encode(FrameMetadata metadata)
        {
            // Nothing to do
        }

        public void PostEncode()
        {
            // Nothing to do
        }

        private void OnOcclusionFrameReceived(AROcclusionFrameEventArgs args)
        {
            // Set texture
            var count = args.textures.Count;
            for (int i = 0; i < count; i++)
            {
                _muxMaterial.SetTexture(args.propertyNameIds[i], args.textures[i]);
            }
            // TODO: calc min/max depth using compute shader
            _muxMaterial.SetVector(k_DepthRange, _depthRange);
        }
    }
}

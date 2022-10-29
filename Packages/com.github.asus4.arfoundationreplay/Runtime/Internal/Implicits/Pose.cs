using System;
using UnityEngine;
using Unity.Mathematics;

namespace ARFoundationReplay
{
    [Serializable]
    public struct Pose : IEquatable<Pose>,
                         IEquatable<UnityEngine.Pose>
    {
        public float3 position;
        public quaternion rotation;

        public bool Equals(Pose o)
        {
            return position.Equals(o.position)
                && rotation.Equals(o.rotation);
        }

        public bool Equals(UnityEngine.Pose o)
        {
            return position.Equals(o.position)
                && rotation.Equals(o.rotation);
        }

        public override string ToString()
        {
            return $"Pose:(p:{position}, r:{rotation})";
        }

        public static implicit operator Pose(UnityEngine.Pose p)
        {
            return new Pose()
            {
                position = p.position,
                rotation = p.rotation,
            };
        }

        public static implicit operator UnityEngine.Pose(Pose p)
        {
            return new UnityEngine.Pose(p.position, p.rotation);
        }

        public static Pose FromTransform(Transform t)
        {
            return new Pose()
            {
                position = t.localPosition,
                rotation = t.localRotation,
            };
        }
    }
}

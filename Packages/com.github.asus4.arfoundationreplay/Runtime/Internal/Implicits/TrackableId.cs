using System;
using System.Runtime.InteropServices;

namespace ARFoundationReplay
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct TrackableId : IEquatable<TrackableId>
    {
        public ulong subId1;
        public ulong subId2;

        public TrackableId(ulong subId1, ulong subId2)
        {
            this.subId1 = subId1;
            this.subId2 = subId2;
        }

        public override readonly int GetHashCode()
        {
            unchecked
            {
                return subId1.GetHashCode() * 486187739 + subId2.GetHashCode();
            }
        }

        public readonly bool Equals(TrackableId o)
        {
            return (subId1 == o.subId1) && (subId2 == o.subId2);
        }

        public override readonly string ToString()
        {
            return string.Format("{0}-{1}",
                subId1.ToString("X16"),
                subId2.ToString("X16"));
        }

        public static implicit operator UnityEngine.XR.ARSubsystems.TrackableId(TrackableId id)
        {
            return new UnityEngine.XR.ARSubsystems.TrackableId(id.subId1, id.subId2);
        }

        public static implicit operator TrackableId(UnityEngine.XR.ARSubsystems.TrackableId id)
        {
            return new TrackableId(id.subId1, id.subId2);
        }
    }
}

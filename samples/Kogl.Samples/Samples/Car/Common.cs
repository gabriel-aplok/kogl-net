// NOTE: this ray-cast car sample is a copied and slightly modified version
// of the jitter2 car example, which does the same with the JigLib vehicle example.

using Jitter2.Collision;
using Jitter2.Collision.Shapes;

namespace Kogl.Samples.Samples.Car;

public static class Common
{
    /// <summary>Broadphase filter to selectively ignore collisions between specific shapes</summary>
    public class IgnoreCollisionBetweenFilter : IBroadPhaseFilter
    {
        private readonly HashSet<ulong> _ignoreMasks = [];

        public bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
        {
            if (proxyA is not RigidBodyShape shapeA || proxyB is not RigidBodyShape shapeB)
            {
                return true; // allow collision processing for non-rigidbody or default scene shapes
            }

            // Always pack IDs in a consistent order (Lowest ID first)
            ulong idA = shapeA.ShapeId;
            ulong idB = shapeB.ShapeId;

            ulong key = idA < idB ? (idA << 32) | idB : (idB << 32) | idA;

            return !_ignoreMasks.Contains(key);
        }

        public void IgnoreCollisionBetween(RigidBodyShape shapeA, RigidBodyShape shapeB)
        {
            ulong idA = shapeA.ShapeId;
            ulong idB = shapeB.ShapeId;

            ulong key = idA < idB ? (idA << 32) | idB : (idB << 32) | idA;

            _ignoreMasks.Add(key);
        }
    }
}

#if SSC_ENTITIES
using Unity.Entities;

// Sci-Fi Ship Controller. Copyright (c) 2018-2024 SCSM Pty Ltd. All rights reserved.
namespace SciFiShipController
{
    public class DOTSHelper
    {
        /// <summary>
        /// Attempt to destroy or clean-up an existing world.
        /// USAGE: DOTSHelper.DestroyWorld(ref sscWorld);
        /// </summary>
        /// <param name="world"></param>
        public static void DestroyWorld(ref World world)
        {
            // To pass in, or return a null, the parameter must be a reference.
            if (world != null)
            {
                world = null;
            }
        }

        /// <summary>
        /// Get the default World in which to instantiate Entities.
        /// USAGE: World sscWorld = DOTSHelper.GetDefaultWorld();
        /// </summary>
        /// <returns></returns>
        public static World GetDefaultWorld()
        {
            #if UNITY_2019_3_OR_NEWER || UNITY_ENTITIES_0_2_0_OR_NEWER
            // Entities 0.2.0+ in U2019.3+
            return World.DefaultGameObjectInjectionWorld;
            #else
            // Entities 0.012-preview.33 - 0.1.1
            return World.Active;
            #endif
        }


        #if SSC_PHYSICS

        /// <summary>
        /// Typically called once to get a reference to the BuildPhysicsWorld for a given World.
        /// USAGE: Unity.Physics.Systems.BuildPhysicsWorld buildPhysicsWorld;
        /// DOTSHelper.GetBuildPhysicsWorld(DOTSHelper.GetDefaultWorld(), ref buildPhysicsWorld);
        /// </summary>
        /// <param name="world"></param>
        /// <param name="buildPhysicsWorld"></param>
        /// <returns></returns>
        public static bool GetBuildPhysicsWorld(World world, ref Unity.Physics.Systems.BuildPhysicsWorld buildPhysicsWorld)
        {
            if (world == null) { return false; }
            else
            {
                #if UNITY_2022_2_OR_NEWER
                UnityEngine.Debug.LogWarning("[ERROR] GetBuildPhysicsWorld() - use SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld for Entities 1.0+ from within a system");
                return false;
                #else
                buildPhysicsWorld = world.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
                return true;
                #endif
            }
        }

        #endif

    }
}
#endif
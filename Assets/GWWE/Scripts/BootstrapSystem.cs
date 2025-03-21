using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

namespace GWWE.Shared.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BootstrapSystem : ISystem
    {
        private EntityQuery _sceneQuery;

        public void OnCreate(ref SystemState state)
        {
            _sceneQuery = state.GetEntityQuery(ComponentType.ReadOnly<SceneReference>());
        }

        public void OnUpdate(ref SystemState state)
        {
            var sceneSystemHandle = World.DefaultGameObjectInjectionWorld.GetExistingSystem<SceneSystem>();
            var unmanagedSceneSystem = World.DefaultGameObjectInjectionWorld.Unmanaged;

            // Always load SharedWorld
            LoadScene(unmanagedSceneSystem, "SharedWorld");

#if UNITY_SERVER
            LoadScene(unmanagedSceneSystem, "ServerWorld");
            Debug.Log("✅ ServerWorld Loaded");
#else
            LoadScene(unmanagedSceneSystem, "ClientWorld");
            Debug.Log("✅ ClientWorld Loaded");
#endif

            state.Enabled = false; // Disable after first run
        }

        private void LoadScene(WorldUnmanaged unmanagedWorld, string sceneName)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var scenes = _sceneQuery.ToEntityArray(Allocator.Temp);
            foreach (var scene in scenes)
            {
                var sceneRef = entityManager.GetComponentData<SceneReference>(scene);

                if (IsSceneMatch(sceneRef.SceneGUID, sceneName))
                {
                    SceneSystem.LoadSceneAsync(unmanagedWorld, sceneRef.SceneGUID, new SceneSystem.LoadParameters { AutoLoad = true });
                    return; // Exit once the scene is loaded
                }
            }

            Debug.LogError($"❌ Failed to load scene: {sceneName}");
        }

        private bool IsSceneMatch(Unity.Entities.Hash128 sceneGUID, string sceneName)
        {
            // Map your known GUIDs to their scene names for comparison
            switch (sceneName)
            {
                case "SharedWorld": return sceneGUID == new Unity.Entities.Hash128("980fc9aa39b2f294a8a20e1ab10e95db");
                case "ServerWorld": return sceneGUID == new Unity.Entities.Hash128("c0f1e1cb1def70f4c939e84765c2b375");
                case "ClientWorld": return sceneGUID == new Unity.Entities.Hash128("648741232b05c834c8187a68d40b9e1f");
                default: return false;
            }
        }
    }
}

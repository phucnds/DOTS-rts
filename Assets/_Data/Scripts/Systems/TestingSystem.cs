using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

partial struct TestingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // int unitCount = 0;
        // foreach (RefRO<Zombie> unit in SystemAPI.Query<RefRO<Zombie>>())
        // {
        //     unitCount++;
        // }

        // Debug.Log(unitCount);
    }

}

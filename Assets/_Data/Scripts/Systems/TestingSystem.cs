using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

partial struct TestingSystem : ISystem
{


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        /*
        int unitCount = 0;
        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRO<UnitMover> unitMover
            )
            in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRO<UnitMover>>().WithPresent<Selected>())
        {

            unitCount++;

        }

        Debug.Log(unitCount);
    */
    }

}

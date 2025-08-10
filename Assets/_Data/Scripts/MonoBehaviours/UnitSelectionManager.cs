using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; private set; }

    public Action OnSelectionAreaStart;
    public Action OnSelectionAreaEnd;

    private Vector2 selectedStartMousePosition;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            selectedStartMousePosition = Input.mousePosition;
            OnSelectionAreaStart?.Invoke();
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 selectionEndMousePosition = Input.mousePosition;

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<Selected>().Build(entityManager);
            NativeArray<Entity> entityArray = entityQuery.ToEntityArray(Allocator.Temp);
            NativeArray<Selected> selectedArray = entityQuery.ToComponentDataArray<Selected>(Allocator.Temp);

            for (int i = 0; i < entityArray.Length; i++)
            {
                entityManager.SetComponentEnabled<Selected>(entityArray[i], false);

                Selected selected = selectedArray[i];
                selected.OnDeselected = true;
                entityManager.SetComponentData(entityArray[i], selected);
            }




            Rect selectionAreaRect = GetSelectionAreaRect();
            float selectionAreaSize = selectionAreaRect.width + selectionAreaRect.height;
            float multipleSelectionSizeMin = 40f;
            bool isMultipleSelection = selectionAreaSize > multipleSelectionSizeMin;

            if (isMultipleSelection)
            {
                entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, Unit>().WithPresent<Selected>().Build(entityManager);
                entityArray = entityQuery.ToEntityArray(Allocator.Temp);

                NativeArray<LocalTransform> localTransformArray = entityQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

                for (int i = 0; i < localTransformArray.Length; i++)
                {
                    LocalTransform unitLocalTransform = localTransformArray[i];

                    Vector2 unitScreenPosition = Camera.main.WorldToScreenPoint(unitLocalTransform.Position);
                    if (selectionAreaRect.Contains(unitScreenPosition))
                    {
                        entityManager.SetComponentEnabled<Selected>(entityArray[i], true);

                        Selected selected = entityManager.GetComponentData<Selected>(entityArray[i]);
                        selected.OnSelected = true;
                        entityManager.SetComponentData(entityArray[i], selected);

                    }
                }
            }
            else
            {
                entityQuery = entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
                PhysicsWorldSingleton physicsWorldSingleton = entityQuery.GetSingleton<PhysicsWorldSingleton>();

                CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

                UnityEngine.Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

                RaycastInput raycastInput = new RaycastInput
                {
                    Start = cameraRay.GetPoint(0f),
                    End = cameraRay.GetPoint(9999f),
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = 1u << GameAssets.UNITS_LAYER,
                        GroupIndex = 0
                    }
                };

                if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit rayCastHit))
                {
                    if (entityManager.HasComponent<Unit>(rayCastHit.Entity))
                    {
                        entityManager.SetComponentEnabled<Selected>(rayCastHit.Entity, true);

                        Selected selected = entityManager.GetComponentData<Selected>(rayCastHit.Entity);
                        selected.OnSelected = true;
                        entityManager.SetComponentData(rayCastHit.Entity, selected);
                    }
                }
            }

            OnSelectionAreaEnd?.Invoke();
        }

        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorldPosition = MouseWorldPosition.Instance.GetPosition();

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            // First, get all selected entities
            using var selectedQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Selected>()
                .Build(entityManager);

            using var selectedEntities = selectedQuery.ToEntityArray(Allocator.Temp);
            
            if (selectedEntities.Length == 0)
            {
                Debug.LogWarning("No selected entities found!");
                return;
            }
            
            Debug.Log($"Found {selectedEntities.Length} selected entities");
            
            // Filter entities that have MoveOverride component
            int validEntityCount = 0;
            for (int i = 0; i < selectedEntities.Length; i++)
            {
                if (entityManager.HasComponent<MoveOverride>(selectedEntities[i]))
                {
                    validEntityCount++;
                }
            }
            
            if (validEntityCount == 0)
            {
                Debug.LogWarning("No selected entities have MoveOverride component!");
                return;
            }
            
            Debug.Log($"Found {validEntityCount} entities with MoveOverride component");
            
            // Generate positions for all valid entities
            using var movePositionArray = GenerateMovePositionArray(mouseWorldPosition, validEntityCount);
            
            int positionIndex = 0;
            for (int i = 0; i < selectedEntities.Length; i++)
            {
                if (entityManager.HasComponent<MoveOverride>(selectedEntities[i]))
                {
                    // Enable MoveOverride component
                    entityManager.SetComponentEnabled<MoveOverride>(selectedEntities[i], true);
                    
                    // Update target position
                    MoveOverride moveOverride = entityManager.GetComponentData<MoveOverride>(selectedEntities[i]);
                    moveOverride.targetPosition = movePositionArray[positionIndex];
                    entityManager.SetComponentData(selectedEntities[i], moveOverride);
                    
                    positionIndex++;
                }
            }
            
            Debug.Log($"Moved {validEntityCount} units to position {mouseWorldPosition}");
        }
    }

    public Rect GetSelectionAreaRect()
    {
        Vector2 selectionEndMousePosition = Input.mousePosition;

        Vector2 lowerLeftCorner = new Vector2(Mathf.Min(selectedStartMousePosition.x, selectionEndMousePosition.x),
                                              Mathf.Min(selectedStartMousePosition.y, selectionEndMousePosition.y));

        Vector2 upperRightCorner = new Vector2(Mathf.Max(selectedStartMousePosition.x, selectionEndMousePosition.x),
                                             Mathf.Max(selectedStartMousePosition.y, selectionEndMousePosition.y));

        return new Rect(
            lowerLeftCorner.x,
            lowerLeftCorner.y,
            upperRightCorner.x - lowerLeftCorner.x,
            upperRightCorner.y - lowerLeftCorner.y);
    }

    private NativeArray<float3> GenerateMovePositionArray(float3 targetPosition, int positionCount)
    {
        NativeArray<float3> positionArray = new NativeArray<float3>(positionCount, Allocator.Temp);

        if (positionCount == 0)
        {
            return positionArray;
        }

        positionArray[0] = targetPosition;

        if (positionCount == 1)
        {
            return positionArray;
        }

        float ringSize = 2.2f;

        int ring = 0;
        int positionIndex = 1;

        while (positionIndex < positionCount)
        {
            int ringPositionCount = 3 + ring * 2;


            for (int i = 0; i < ringPositionCount; i++)
            {
                float angle = i * (math.PI2 / ringPositionCount);
                float3 ringVector = math.rotate(quaternion.RotateY(angle), new float3(ringSize * (ring + 1), 0, 0));
                float3 ringPosition = targetPosition + ringVector;

                positionArray[positionIndex] = ringPosition;
                positionIndex++;

                if (positionIndex >= positionCount)
                {
                    break;
                }
            }

            ring++;
        }


        return positionArray;
    }
}

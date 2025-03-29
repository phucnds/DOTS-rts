using System;
using Unity.Collections;
using Unity.Entities;
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
            
            for (int i = 0; i < entityArray.Length; i++)
            {
                entityManager.SetComponentEnabled<Selected>(entityArray[i], false);
            }

           

            Rect selectionAreaRect = GetSelectionAreaRect();
            float selectionAreaSize = selectionAreaRect.width + selectionAreaRect.height;
            float multipleSelectionSizeMin = 40f;
            bool isMultipleSelection = selectionAreaSize > multipleSelectionSizeMin;
            
            if(isMultipleSelection)
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
                    }
                }
            }
            else
            {

            }

            OnSelectionAreaEnd?.Invoke();
        }

        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePosition = MouseWorldPosition.Instance.GetPosition();

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<UnitMover, Selected>().Build(entityManager);

            NativeArray<Entity> entityArray = entityQuery.ToEntityArray(Allocator.Temp);
            NativeArray<UnitMover> unitMoverArray = entityQuery.ToComponentDataArray<UnitMover>(Allocator.Temp);

            for (int i = 0; i < unitMoverArray.Length; i++)
            {
                UnitMover unitMover = unitMoverArray[i];
                unitMover.targetPosition = mousePosition;
                unitMoverArray[i] = unitMover;
            }

            entityQuery.CopyFromComponentDataArray(unitMoverArray);
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
}

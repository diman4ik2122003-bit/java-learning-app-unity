using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ElevatorLevelController : MonoBehaviour
{
    [System.Serializable]
    public class ElevatorData
    {
        public int id;
        public float requiredWeight;
        public Transform elevatorTransform;
        public Transform targetPosition;
        public bool isLowered;
        public GameObject weightVisual; // Optional: change visual when weight is added
    }

    public List<ElevatorData> elevators = new List<ElevatorData>();
    public PlayerController player;
    public float moveSpeed = 2f;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    public IEnumerator LowerElevator(int id, long weight)
    {
        ElevatorData elevator = elevators.Find(e => e.id == id);
        if (elevator == null)
        {
            Debug.LogError($"[ElevatorController] Elevator with id {id} not found!");
            yield break;
        }

        if (elevator.isLowered)
        {
            Debug.Log($"[ElevatorController] Elevator {id} already lowered.");
            yield break;
        }

        if (weight >= elevator.requiredWeight)
        {
            Debug.Log($"[ElevatorController] Lowering elevator {id} with weight {weight}");
            elevator.isLowered = true;
            
            // Move elevator to target position
            Vector3 startPos = elevator.elevatorTransform.position;
            Vector3 endPos = elevator.targetPosition.position;
            float duration = Vector3.Distance(startPos, endPos) / moveSpeed;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elevator.elevatorTransform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            elevator.elevatorTransform.position = endPos;

            // After elevator is lowered, move player to it (simulation of progress)
            yield return MovePlayerToElevator(elevator);
        }
        else
        {
            Debug.Log($"[ElevatorController] Weight {weight} is not enough for elevator {id} (required {elevator.requiredWeight})");
            // Highlight error or show visual feedback
        }
    }

    private IEnumerator MovePlayerToElevator(ElevatorData elevator)
    {
        if (player != null)
        {
            // Simple simulation: move right a bit
            yield return player.MoveRightCoroutine(2);
        }
    }

    public void ResetLevel()
    {
        foreach (var elevator in elevators)
        {
            elevator.isLowered = false;
            // Move back to spawn/initial position if needed
        }
    }
}

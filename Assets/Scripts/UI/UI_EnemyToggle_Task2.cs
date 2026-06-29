using UnityEngine;

public class UI_EnemyDebugToggle : MonoBehaviour
{
    // Drag your Enemy game object (the one with the EnemyLocomotion script) here
    [SerializeField] private EnemyLocomotion enemyLocomotion; 
    
    private bool isRaysVisible = true;

    public void ToggleRays()
    {
        if (enemyLocomotion != null)
        {
            // Invert the current state
            isRaysVisible = !isRaysVisible; 
            enemyLocomotion.SetDebugRaysVisible(isRaysVisible);
        }
    }

    // ????
}
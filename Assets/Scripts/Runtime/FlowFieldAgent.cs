using UnityEngine;

/// <summary>
/// Represents an agent that navigates the world using a Flow Field.
/// This component should be attached to a GameObject that also has a Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FlowFieldAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The maximum force applied to the agent to follow the flow field.")]
    public float moveSpeed = 100f;

    [Tooltip("The maximum velocity the agent can reach.")]
    public float maxVelocity = 4f;

    // Private components and references
    private Rigidbody rb;
    private FlowFieldManager flowFieldManager;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("FlowFieldAgent requires a Rigidbody component.", this);
        }
    }

    /// <summary>
    /// Initializes the agent with a reference to the FlowFieldManager.
    /// This method must be called after the agent is instantiated.
    /// </summary>
    /// <param name="manager">The active FlowFieldManager.</param>
    public void Initialize(FlowFieldManager manager)
    {
        flowFieldManager = manager;
    }

    /// <summary>
    /// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
    /// Used for physics-based movement.
    /// </summary>
    void FixedUpdate()
    {
        // Do nothing if the flow field is not available
        if (flowFieldManager == null || flowFieldManager.flowFieldView == null)
        {
            return;
        }

        // Stop movement if the current velocity is too high
        if (rb.velocity.magnitude > maxVelocity)
        {
            return;
        }

        // Get the size of each cell from the view component
        float cellSize = flowFieldManager.flowFieldView.cellSize;

        // Convert the agent's world position to fractional grid coordinates
        float gridX = transform.position.x / cellSize;
        float gridY = transform.position.z / cellSize;

        // Get the smoothed flow direction from the manager for the current position
        Vector2 direction2D = flowFieldManager.GetSmoothFlowDirection(gridX, gridY);

        // Apply force if the direction is valid
        if (direction2D != Vector2.zero)
        {
            // Convert the 2D direction vector into a 3D force vector
            Vector3 moveDirection = new Vector3(direction2D.x, 0, direction2D.y);

            // Apply force to the Rigidbody
            rb.AddForce(moveDirection * moveSpeed * Time.fixedDeltaTime, ForceMode.Force);
        }
    }
}
using UnityEngine;

// Adapter for Unity Input System to provide decoupled input to controllers
// Responsibilities:
// - Read move vector (WASD), fire (LMB), reload (RMB), roll (Space), interact (E), skill (Q)
// - Expose input state via properties/events for PlayerController
// - No gameplay logic or physics here
// SOLID:
// - ISP/DIP: Provide narrow, testable input surface

public class InputHandler : MonoBehaviour
{
    // [Bindings] Wire to InputAction asset (InputSystem_Actions)
    // [State] Current movement vector and button states
    // [Events] Fire input events for pressed/held/released where needed
}



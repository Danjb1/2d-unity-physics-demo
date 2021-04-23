using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Generates player intentions based on user input.
 */
public class HumanPlayerController : MonoBehaviour
{

  private EntityInput input;

  void Start()
  {
    input = GetComponent<EntityInput>();
  }

  void Update()
  {
    CheckHorizontalInput();
    CheckJumpInput();
  }

  void CheckHorizontalInput()
  {
    float dirPressed = Input.GetAxisRaw("Horizontal");
    if (dirPressed < 0)
    {
      input.DirX = -1;
    }
    else if (dirPressed > 0)
    {
      input.DirX = 1;
    }
    else
    {
      input.DirX = 0;
    }
  }

  void CheckJumpInput()
  {
    input.JumpHeld = Input.GetButton("Jump");
  }

}

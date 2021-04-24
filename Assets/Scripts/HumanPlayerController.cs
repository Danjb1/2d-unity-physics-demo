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
      input.DirX = DirX.Left;
    }
    else if (dirPressed > 0)
    {
      input.DirX = DirX.Right;
    }
    else
    {
      input.DirX = DirX.None;
    }
  }

  void CheckJumpInput()
  {
    input.JumpHeld = Input.GetButton("Jump");
  }

}

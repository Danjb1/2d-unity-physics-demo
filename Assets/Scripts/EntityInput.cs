using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DirXMethods
{
  public static int GetMultiplier(this DirX dir)
  {
    if (dir == DirX.Left)
    {
      return -1;
    }
    else if (dir == DirX.Right)
    {
      return 1;
    }
    return 0;
  }
}

public enum DirX { Left, None, Right }

/**
 * Represents the intentions and movement properties of an entity.
 *
 * This is kept separate from the logic that populates it, so that it can be
 * shared by both human- and AI-controlled entities.
 */
public class EntityInput : MonoBehaviour
{

  /**
   * This Entity's horizontal acceleration (m/s^2).
   */
  public float acceleration = 60;

  /**
   * The maximum jump height, assuming jump is pressed for a single frame.
   *
   * In reality, the maximum jump height will depend on `maxJumpTime`, as this
   * allows a jump to be extended for multiple frames.
   */
  public float maxJumpHeight = 0.5f;

  /**
   * The maximum time jump can be held to extend a jump, in seconds.
   */
  public float maxJumpTime = 0.25f;

  /**
   * Absolute horizontal deceleration due to friction when airborne (m/s^2).
   */
  public float airFriction = 40;

  /**
   * Absolute horizontal deceleration due to friction when grounded (m/s^2).
   */
  public float groundFriction = 90;

  /**
   * The intended movement direction in the x-axis.
   *
   * -1 is left, 0 is no movement and 1 is right.
   */
  public DirX DirX { get; set; } = DirX.None;

  /**
   * Whether the entity is initiating or continuing a jump.
   */
  public bool JumpHeld { get; set; }

}

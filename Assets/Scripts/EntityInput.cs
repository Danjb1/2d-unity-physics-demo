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
   * This Entity's horizontal acceleration.
   */
  public float acceleration = 1;

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

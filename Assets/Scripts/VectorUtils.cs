using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorUtils
{

  /**
   * Returns a copy of the given vector with a new x-value.
   */
  public static Vector2 SetX(Vector2 v, float newX)
  {
    return new Vector2(newX, v.y);
  }

  /**
   * Returns a copy of the given vector with a new y-value.
   */
  public static Vector2 SetY(Vector2 v, float newY)
  {
    return new Vector2(v.x, newY);
  }

  /**
   * Returns the given vector, scaled such that its width (horizontal component)
   * matches the given value.
   */
  public static Vector2 ScaleToWidth(Vector2 v, float desiredWidth)
  {
    return v / v.x * desiredWidth;
  }

  /**
   * Returns the given vector, scaled such that its height (vertical component)
   * matches the given value.
   */
  public static Vector2 ScaleToHeight(Vector2 v, float desiredHeight)
  {
    return v / v.y * desiredHeight;
  }

}
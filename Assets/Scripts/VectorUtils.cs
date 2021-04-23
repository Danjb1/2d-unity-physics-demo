using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorUtils
{

  public static Vector2 SetX(Vector2 v, float newX)
  {
    return new Vector2(newX, v.y);
  }

  public static Vector2 SetY(Vector2 v, float newY)
  {
    return new Vector2(v.x, newY);
  }

}
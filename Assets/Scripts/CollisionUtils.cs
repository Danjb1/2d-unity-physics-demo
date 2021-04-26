using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CollisionUtils
{

  /**
   * Determines if the given collision is valid.
   */
  public static bool IsCollisionValid(RaycastHit2D hit)
  {
    if (hit.collider.gameObject.CompareTag("One Way Platform")
        && hit.normal != Vector2.up)
    {
      // Only collisions with the top of a one-way platform are valid
      return false;
    }
    return true;
  }

  /**
   * Determines if a collision is valid for the given collision node.
   *
   * `node` is a position relative to the center of the hitbox, and `extents`
   * is the distance from the center of that hitbox to its edges.
   */
  public static bool CanNodeCollide(
      Vector2 node, RaycastHit2D hit, Vector2 extents, float slopeTolerance)
  {
    if (IsCollisionWithFloor(hit, slopeTolerance))
    {
      // Only nodes at the bottom of the entity can collide with floors
      return node.y == -extents.y;
    }
    else if (IsCollisionWithCeiling(hit))
    {
      // Only nodes at the top of the entity can collide with ceilings
      return node.y == extents.y;
    }
    else if (IsCollisionWithRightWall(hit))
    {
      // Only nodes at the right of the entity can collide with right walls
      return node.x == extents.x;
    }
    else if (IsCollisionWithLeftWall(hit))
    {
      // Only nodes at the left of the entity can collide with left walls
      return node.x == -extents.x;
    }
    else
    {
      // We have collided with a non-traversable slope. For now, since our scene
      // contains only sloped floors (not sloped ceilings), only allow nodes at
      // the bottom of the entity to collide.
      return node.y == -extents.y;
    }
  }

  /**
   * Calculates the movement vector to apply in order to escape a solid in which
   * we have become embedded, given the collision that should have occurred, the
   * previous position and the current (embedded) position.
   *
   * The resulting vector is in the direction of the collision normal. This
   * ensures that we preserve as much of the original movement as possible. For
   * example, if we hit a floor while moving diagonally, we still want the
   * horizontal motion to be applied.
   */
  public static Vector2 CalculateCollisionEscapeVector(
      Vector2 currentPosition,
      Vector2 prevPosition,
      RaycastHit2D hit,
      float slopeTolerance)
  {
    // The journey we have just taken that has left us embedded in a solid
    Vector2 journey = currentPosition - prevPosition;

    // Distance travelled inside the solid (after the collision occurred)
    float distanceTravelledInSolid = journey.magnitude - hit.distance;

    // The portion of the journey for which we were embedded in the solid
    Vector2 journeyInSolid = journey.normalized * distanceTravelledInSolid;

    Vector2 escapeVector;

    if (IsCollisionWithSlope(hit))
    {
      // We have collided with a sloped floor. We need to treat this as a
      // special case, because if we moved the entity in the direction of the
      // normal then they would slide down the slope (which we don't want).
      escapeVector = CalculateCollisionEscapeVectorForSlope(
          journeyInSolid, hit, slopeTolerance);
    }
    else
    {
      // Negating `journeyInSolid` gives us the journey we would need to take to
      // get back OUT of the solid. However, we don't want to just retrace our
      // steps - we want to escape in the direction of the collision normal - so
      // we have to project this outward journey onto the normal.
      escapeVector = Vector3.Project(-journeyInSolid, hit.normal);
    }

    // Add a tiny bit extra, just to make sure we are clear of the solid
    return escapeVector
        + escapeVector.normalized * EntityPhysics.SmallestDistance;
  }

  /**
   * Given a journey we have just taken that has embedded us in a slope, and the
   * collision with that slope, calculates the movement vector to apply in order
   * to position this entity atop the slope.
   */
  public static Vector2 CalculateCollisionEscapeVectorForSlope(
      Vector2 journeyInSlope, RaycastHit2D hit, float slopeTolerance)
  {
    if (IsCollisionWithFloor(hit, slopeTolerance))
    {
      return CalculateCollisionEscapeVectorForTraversableSlope(
          journeyInSlope, hit);
    }
    else
    {
      Vector2 vec = CalculateCollisionEscapeVectorForNonTraversableSlope(
          journeyInSlope, hit);
      return vec;
    }
  }

  public static Vector2 CalculateCollisionEscapeVectorForTraversableSlope(
      Vector2 journeyInSlope, RaycastHit2D hit)
  {
    // We want to move directly up, so that we are sitting atop the slope. This
    // means we need to calculate the vertical distance between the endpoint of
    // our journey and the point directly above it that lies on the surface of
    // the slope.

    // First we need a vector representing the slope itself
    Vector2 slope = Vector2.Perpendicular(hit.normal);

    // Flip this vector if necessary, so that it's pointing in the direction of
    // horizontal travel.
    if (Mathf.Sign(slope.x) != Mathf.Sign(journeyInSlope.x))
    {
      slope = -slope;
    }

    // To find the desired point on the slope, we need to scale our slope vector
    // until it covers the same x-distance as `journeyInSlope`.
    Vector2 scaledSlope = VectorUtils.ScaleToWidth(slope, journeyInSlope.x);

    // We want a vector that connects the endpoint of our journey to the
    // endpoint of this `scaledSlope` vector. This should point straight up.
    return scaledSlope - journeyInSlope;
  }

  public static Vector2 CalculateCollisionEscapeVectorForNonTraversableSlope(
      Vector2 journeyInSlope, RaycastHit2D hit)
  {
    // This is similar to the traversable case, but instead of moving up we want
    // to move directly sideways until we are sitting atop the slope. This
    // ensures that we can never ascend the slope, because any vertical progress
    // is effectively disgarded.

    // First we need a vector representing the slope itself
    Vector2 slope = Vector2.Perpendicular(hit.normal);

    // Flip this vector if necessary, so that it's pointing in the OPPOSITE
    // direction of horizontal travel.
    if (Mathf.Sign(slope.x) == Mathf.Sign(journeyInSlope.x))
    {
      slope = -slope;
    }

    // To find the desired point on the slope, we need to scale our slope vector
    // until it covers the same y-distance as `journeyInSlope`.
    Vector2 scaledSlope = VectorUtils.ScaleToHeight(slope, journeyInSlope.y);

    // We want a vector that connects the endpoint of our journey to the
    // endpoint of this `scaledSlope` vector. This should point horizontally.
    return scaledSlope - journeyInSlope;
  }

  /**
   * Determines if a collision is with a sloped floor.
   */
  public static bool IsCollisionWithSlope(RaycastHit2D hit)
  {
    return hit.normal != Vector2.up && hit.normal.y > 0;
  }

  /**
   * Determines if a collision is with a floor (including sloped floors, as
   * long as they are traversable).
   */
  public static bool IsCollisionWithFloor(
      RaycastHit2D hit, float slopeTolerance)
  {
    return hit.normal.y > slopeTolerance;
  }

  /**
   * Determines if a collision is with a ceiling.
   */
  public static bool IsCollisionWithCeiling(RaycastHit2D hit)
  {
    return hit.normal == Vector2.down;
  }

  /**
   * Determines if a collision is with a wall.
   */
  public static bool IsCollisionWithWall(RaycastHit2D hit, float slopeTolerance)
  {
    return IsCollisionWithLeftWall(hit) || IsCollisionWithRightWall(hit)
        // Steep slopes are treated as walls for collision purposes
        || (IsCollisionWithSlope(hit)
            && !IsCollisionWithFloor(hit, slopeTolerance));
  }

  /**
   * Determines if a collision is with a left wall.
   */
  public static bool IsCollisionWithLeftWall(RaycastHit2D hit)
  {
    return hit.normal == Vector2.right;
  }

  /**
   * Determines if a collision is with a right wall.
   */
  public static bool IsCollisionWithRightWall(RaycastHit2D hit)
  {
    return hit.normal == Vector2.left;
  }

}

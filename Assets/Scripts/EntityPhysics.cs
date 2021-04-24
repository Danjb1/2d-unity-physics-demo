using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Applies physics to the parent GameObject.
 *
 * This includes gravity, acceleration, friction and collisions with level
 * geometry.
 */
public class EntityPhysics : MonoBehaviour
{

  /**
   * Change in vertical velocity to apply each tick.
   */
  private const float Gravity = -1f;

  /**
   * Absolute change in horizontal velocity to apply each tick as result of
   * friction, when airborne.
   */
  private const float AirFriction = 0.65f;

  /**
   * Absolute change in horizontal velocity to apply each tick as result of
   * friction, when grounded.
   */
  private const float GroundFriction = 1.5f;

  /**
   * Maximum magnitude of an entity's x-velocity (m/s).
   *
   * If this is set too high, entities may pass through thin level geometry.
   */
  private const float MaxSpeedX = 12f;

  /**
   * Maximum fall speed that can be attained by gravity alone (m/s).
   *
   * If this is set too high, entities may pass through thin level geometry.
   */
  private const float TerminalVelocity = -14f;

  /**
   * The smallest possible distance that matters, as far as our physics are
   * concerned.
   *
   * Distances smaller than this can safely be rounded to zero.
   */
  private const float SmallestDistance = 0.0001f;

  /**
   * How far a "stuck" entity should move, in the hope of becoming unstuck.
   */
  private const float UnstuckDistance = 0.15f;

  /**
   * Input that governs this entity's movement.
   */
  private EntityInput input;

  /**
   * Rigidbody used to move the entity with collision detection.
   */
  new private Rigidbody2D rigidbody;

  /**
   * Whether the entity is currently on the ground.
   *
   * This flag is cleared at the end of `FixedUpdate`, and set again by any
   * collisions that follow.
   */
  private bool grounded;

  /**
   * Position at the end of the previous frame.
   */
  private Vector2 prevPosition;

  /**
   * Points used to check for collisions, relative to the entity's center.
   *
   * For small entities, using the corners of the hitbox should be sufficient to
   * detect all collisions. For larger entities - or if the level geometry
   * contains very thin spikes - additional collision nodes may be necessary.
   *
   * The more collision nodes, the more accurate our collision detection will
   * be, but also the more expensive it becomes, as each node entails a raycast.
   *
   * If the entity changes size, this array must be regenerated.
   */
  private Vector2[] collisionNodes;

  /**
   * SpriteRenderer used to retrieve the entity bounds.
   */
  private SpriteRenderer spriteRenderer;

  void Start()
  {
    input = GetComponent<EntityInput>();
    rigidbody = GetComponent<Rigidbody2D>();
    spriteRenderer = GetComponent<SpriteRenderer>();

    // For now, all entities use the same collision nodes, but later we could
    // optimise this by taking into account the size of the entity.
    collisionNodes = CreateCollisionNodes();
  }

  private Vector2[] CreateCollisionNodes()
  {
    return new Vector2[] {
      // Center-left
      new Vector2(-spriteRenderer.bounds.extents.x, 0),
      // Center-right
      new Vector2(spriteRenderer.bounds.extents.x, 0),
      // Top-left
      new Vector2(
          -spriteRenderer.bounds.extents.x,
          spriteRenderer.bounds.extents.y),
      // Top-right
      new Vector2(
          spriteRenderer.bounds.extents.x,
          spriteRenderer.bounds.extents.y),
      // Bottom-right
      new Vector2(
          spriteRenderer.bounds.extents.x,
          -spriteRenderer.bounds.extents.y),
      // Bottom-left
      new Vector2(
          -spriteRenderer.bounds.extents.x,
          -spriteRenderer.bounds.extents.y)
    };
  }

  void FixedUpdate()
  {
    ApplyAcceleration();
    ApplyJump();
    ApplyGravity();
    ApplyFriction();
    RememberPosition();
    ScheduleMove();
  }

  /**
   * Increases the horizontal velocity according to the entity input.
   */
  private void ApplyAcceleration()
  {
    rigidbody.velocity = VectorUtils.SetX(
      rigidbody.velocity,
      Mathf.Clamp(rigidbody.velocity.x
          + input.acceleration * input.DirX.GetMultiplier(),
          -MaxSpeedX, MaxSpeedX)
    );
  }

  /**
   * Initiates or continues a jump based on entity input.
   */
  private void ApplyJump()
  {
    if (grounded && input.JumpHeld)
    {
      // Simple jump, for now
      rigidbody.velocity = VectorUtils.SetY(
          rigidbody.velocity,
          rigidbody.velocity.y + 15
      );
      grounded = false;
    }
  }

  /**
   * Reduces horizontal velocity according to friction.
   */
  private void ApplyFriction()
  {
    if (!ShouldApplyFriction())
    {
      return;
    }

    float friction = grounded ? GroundFriction : AirFriction;

    // Apply friction as a drag force in the opposite direction of movement
    if (rigidbody.velocity.x < 0)
    {
      rigidbody.velocity = VectorUtils.SetX(
          rigidbody.velocity,
          Mathf.Min(rigidbody.velocity.x + friction, 0)
      );
    }
    else if (rigidbody.velocity.x > 0)
    {
      rigidbody.velocity = VectorUtils.SetX(
          rigidbody.velocity,
          Mathf.Max(rigidbody.velocity.x - friction, 0)
      );
    }
  }

  private bool ShouldApplyFriction()
  {
    // Only apply friction if no direction is held...
    return rigidbody.velocity.x == 0 ||
        // ... or we are trying to change direction
        Mathf.Sign(rigidbody.velocity.x) != input.DirX.GetMultiplier();
  }

  /**
   * Changes the velocity by applying gravity.
   */
  private void ApplyGravity()
  {
    rigidbody.velocity = VectorUtils.SetY(
        rigidbody.velocity,
        Mathf.Max(rigidbody.velocity.y + Gravity, TerminalVelocity)
    );
  }

  /**
   * Tells the Rigidbody how it should move after FixedUpdate has finished.
   *
   * This movement will result in the OnTrigger* callbacks below, should we
   * collide with any trigger colliders.
   */
  private void ScheduleMove()
  {
    // Reset this flag to prepare for imminent collisions
    grounded = false;

    rigidbody.MovePosition((Vector2)transform.position
        + rigidbody.velocity * Time.fixedDeltaTime);
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.gameObject.CompareTag("Solid"))
    {
      CollideWithSolid(other);
    }
  }

  private void OnTriggerStay2D(Collider2D other)
  {
    if (other.gameObject.CompareTag("Solid"))
    {
      CollideWithSolid(other);
    }
  }

  /**
   * Resolves a collision with the given collider.
   *
   * In hindsight, it may have been easier to look for collisions BEFORE
   * attempting the movement, but I am too far down the rabbit hole to change it
   * now.
   *
   * That approach would allow us to consider all collisions ahead of time and
   * then decide how to resolve them, whereas this approach means that we have
   * to resolve collisions as they occur, without knowing what other collisions
   * may have occurred simultaneously.
   */
  private void CollideWithSolid(Collider2D other)
  {
    float distanceTravelled = (rigidbody.position - prevPosition).magnitude;

    // We know that a collision has occurred but we don't know where exactly.
    // To find the position of the collision, we re-enact the movement that's
    // just occurred using raycasts from each of our collision nodes.
    foreach (Vector2 node in collisionNodes)
    {
      Vector2 prev = prevPosition + node;
      Vector2 current = rigidbody.position + node;
      RaycastHit2D hit = Physics2D.Raycast(
          prev, current - prev, distanceTravelled, LayerMask.GetMask("Level"));

      // We only care about collisions with the collider that caused this
      // callback; other colliders can take care of their own collisions.
      if (hit.collider == other && CanCollide(node, hit))
      {
        ProcessCollision(hit);
        return;
      }
    }
  }

  /**
   * Determines if a collision is valid for the given collision node.
   */
  private bool CanCollide(Vector2 node, RaycastHit2D hit)
  {

    if (hit.normal == Vector2.up)
    {
      // Only nodes at the bottom of the entity can collide with floors
      return node.y == -spriteRenderer.bounds.extents.y;
    }
    else if (hit.normal == Vector2.down)
    {
      // Only nodes at the top of the entity can collide with ceilings
      return node.y == spriteRenderer.bounds.extents.y;
    }
    else if (hit.normal == Vector2.left)
    {
      // Only nodes at the right of the entity can collide with right walls
      return node.x == spriteRenderer.bounds.extents.x;
    }
    else if (hit.normal == Vector2.right)
    {
      // Only nodes at the left of the entity can collide with left walls
      return node.x == -spriteRenderer.bounds.extents.x;
    }
    // We have collided with a slope. For now, just allow all nodes to collide
    // with slopes. Later, we could be smarter about this by considering the
    // angle of the slope.
    return true;
  }

  /**
   * Moves the entity away from the given collision until it is no longer
   * colliding, and changes the velocity as appropriate.
   */
  private void ProcessCollision(RaycastHit2D hit)
  {
    // Calculate how far we have become embedded in the solid
    Vector2 escapeVector = CalculateCollisionEscapeVector(hit);

    // Move away from the collision
    rigidbody.transform.position += (Vector3)escapeVector;

    // Adjust velocity
    if (hit.normal == Vector2.up || hit.normal.y > 0)
    {
      // Floors / slopes
      rigidbody.velocity = VectorUtils.SetY(rigidbody.velocity, 0);
      grounded = true;
    }
    else if (hit.normal == Vector2.down)
    {
      // Ceilings
      rigidbody.velocity = VectorUtils.SetY(rigidbody.velocity, 0);
    }
    else if (hit.normal == Vector2.left || hit.normal == Vector2.right)
    {
      // Walls
      rigidbody.velocity = VectorUtils.SetX(rigidbody.velocity, 0);
    }
  }

  /**
   * Calculates the movement vector to apply in order to escape a solid in which
   * we have become embedded, given the collision that should have occurred and
   * knowing where we are now.
   *
   * The resulting vector is in the direction of the collision normal. This
   * ensures that we preserve as much of the original movement as possible. For
   * example, if we hit a floor while moving diagonally, we still want the
   * horizontal motion to be applied.
   */
  private Vector2 CalculateCollisionEscapeVector(RaycastHit2D hit)
  {
    if (hit.distance == 0)
    {
      // We are already inside the solid, so we can't rely on the collision
      // normal. If this happens, something has gone wrong. For now, just try
      // moving up and hope that we escape the collision.
      return Vector2.up * UnstuckDistance;
    }

    // The journey we have just taken that has left us embedded in a solid
    Vector2 journey = rigidbody.position - prevPosition;

    // Distance travelled inside the solid (after the collision occurred)
    float distanceTravelledInSolid = journey.magnitude - hit.distance;

    // The portion of the journey for which we were embedded in the solid
    Vector2 journeyInSolid = journey.normalized * distanceTravelledInSolid;

    if (ShouldClimbSlope(hit))
    {
      // We have collided with a sloped floor. We need to treat this as a
      // special case, because if we moved the entity in the direction of the
      // normal then they would slide down the slope (which we don't want).
      return CalculateCollisionEscapeVectorForSlope(journeyInSolid, hit);
    }

    // Negating `journeyInSolid` gives us the journey we would need to take to
    // get back OUT of the solid. However, we don't want to just retrace our
    // steps - we want to escape in the direction of the collision normal - so
    // we have to project this outward journey onto the normal.
    Vector2 escapeVector = Vector3.Project(-journeyInSolid, hit.normal);

    // We add a tiny bit extra, just to make sure we are clear of the solid
    return escapeVector + hit.normal * SmallestDistance;
  }

  /**
   * Given a journey we have just taken that has embedded us in a slope, and the
   * collision with that slope, calculates the movement vector to apply in order
   * to position this entity atop the slope.
   */
  private Vector2 CalculateCollisionEscapeVectorForSlope(
      Vector2 journeyInSlope, RaycastHit2D hit)
  {
    // We want to move directly up, so that we are sitting atop the slope. This
    // means we need to calculate the vertical distance between the endpoint of
    // our journey and the point directly above it that lies on the surface of
    // the slope. First we need a vector representing the slope itself
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
    Vector2 escapeVector = scaledSlope - journeyInSlope;

    // We add a tiny bit extra, just to make sure we are clear of the slope
    return escapeVector + Vector2.up * SmallestDistance;
  }

  /**
   * Given a collision with a slope, determines if it should be ascended.
   */
  private bool ShouldClimbSlope(RaycastHit2D hit)
  {
    // For now, all slopes can be climbed
    return hit.normal != Vector2.up && hit.normal.y > 0;
  }

  /**
   * Store the current position for use in any imminent collisions.
   */
  private void RememberPosition()
  {
    prevPosition = transform.position;
  }

}

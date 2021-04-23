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
   * Maximum magnitude of an entity's y-velocity (m/s).
   *
   * If this is set too high, entities may pass through thin level geometry.
   */
  private const float MaxSpeedX = 50f;

  /**
   * Maximum magnitude of an entity's y-velocity (m/s).
   *
   * If this is set too high, entities may pass through thin level geometry.
   */
  private const float MaxSpeedY = 5f;

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
  private const float UnstuckDistance = 0.25f;

  /**
   * Input that governs this entity's movement.
   */
  private EntityInput input;

  /**
   * Rigidbody used to move the entity with collision detection.
   */
  new private Rigidbody2D rigidbody;

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
    ApplyInput();
    ApplyGravity();
    RememberPosition();
    ScheduleMove();
  }

  /**
   * Changes the velocity according to the entity input.
   */
  void ApplyInput()
  {
    rigidbody.velocity = VectorUtils.SetX(
      rigidbody.velocity,
      Mathf.Clamp(rigidbody.velocity.x + input.acceleration * input.DirX,
          -MaxSpeedX, MaxSpeedX)
    );
  }

  /**
   * Changes the velocity by applying gravity.
   */
  void ApplyGravity()
  {
    rigidbody.velocity = VectorUtils.SetY(
        rigidbody.velocity,
        Mathf.Clamp(rigidbody.velocity.y + Gravity, -MaxSpeedY, MaxSpeedY)
    );
  }

  /**
   * Tells the Rigidbody how it should move after FixedUpdate has finished.
   *
   * This movement will result in the OnTrigger* callbacks below, should we
   * collide with any trigger colliders.
   */
  void ScheduleMove()
  {
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
      if (hit.collider == other)
      {
        ProcessCollision(hit);
        return;
      }
    }
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
    if (hit.normal == Vector2.up || hit.normal == Vector2.down)
    {
      // Floors / ceilings
      rigidbody.velocity = VectorUtils.SetY(rigidbody.velocity, 0);
    }
    else if (hit.normal == Vector2.left || hit.normal == Vector2.right)
    {
      // Walls
      rigidbody.velocity = VectorUtils.SetX(rigidbody.velocity, 0);
    }
  }

  /**
   * Calculates the distance we need to move in order to escape a solid in which
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

    // Negating `journeyInSolid` gives us the journey we would need to take to
    // get back OUT of the solid. However, we don't want to just retrace our
    // steps - we want to escape in the direction of the collision normal - so
    // we have to project this outward journey onto the normal.
    Vector2 escapeVector = Vector3.Project(-journeyInSolid, hit.normal);

    // We add a tiny bit extra, just to make sure we are clear of the solid
    return escapeVector + hit.normal * SmallestDistance;
  }

  /**
   * Store the current position for use in any imminent collisions.
   */
  private void RememberPosition()
  {
    prevPosition = transform.position;
  }

}

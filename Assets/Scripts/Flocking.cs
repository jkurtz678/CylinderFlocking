using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Flocking : MonoBehaviour
{
    public Vector3 baseRotation;

    [Range(0, 10)]
    public float maxSpeed = 1f;

    [Range(.1f, .5f)]
    public float maxForce = .03f;

    [Range(1, 10)]
    public float neighborhoodRadius = 3f;

    [Range(0, 3)]
    public float separationAmount = 1f;

    [Range(0, 3)]
    public float cohesionAmount = 1f;

    [Range(0, 3)]
    public float alignmentAmount = 1f;

    [Range(0, 5)]
    public float guideAmount = 4f;

    [Range(0, 10)]
    public float guideDistance = 2f;

    public Vector2 acceleration;
    public Vector2 velocity;
    public GameObject[] guideObjects;


    //wrapping stuff
    public float screenWidth;
    public float screenHeight;
    Transform[] ghosts = new Transform[2];

    private Vector2 Position {
        get {
            return gameObject.transform.position;
        }
        set {
            gameObject.transform.position = value;
        }
    }

    private void Start()
    {
        float angle = Random.Range(0, 2 * Mathf.PI);
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle) + baseRotation);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        guideObjects = GameObject.FindGameObjectsWithTag("Guide");
        Debug.Log("num guide objects:" + guideObjects.Count());
        var cam = Camera.main;

        var screenBottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, transform.position.z));
        var screenTopRight = cam.ViewportToWorldPoint(new Vector3(1, 1, transform.position.z));

        screenWidth = screenTopRight.x - screenBottomLeft.x;
        screenHeight = screenTopRight.y - screenBottomLeft.y;
        CreateGhosts();
        PositionGhosts();
    }

    private void Update()
    {
        var boidColliders = Physics2D.OverlapCircleAll(Position, neighborhoodRadius);
        List<Flocking> boids = new List<Flocking>();
        foreach( var boidCollider in boidColliders)
        {
            if(boidCollider.GetComponent<Flocking>() == null)
            {
                boids.Add(boidCollider.GetComponent<GhostFlock>().parentBoid);
            }
            else
            {
                boids.Add(boidCollider.GetComponent<Flocking>());
            }
        }
        //var boids = boidColliders.Select(o => o.GetComponent<Flocking>()).ToList();
        boids.Remove(this);

        Flock(boids);
        UpdateVelocity();
        UpdatePosition();
        UpdateRotation();
        //WrapAround();
        SwapShips();
    }
    void CreateGhosts()
    {
        for (int i = 0; i < 2; i++)
        {
            ghosts[i] = Instantiate(transform, Vector3.zero, Quaternion.identity) as Transform;

            DestroyImmediate(ghosts[i].GetComponent<Flocking>());
            GhostFlock gf = ghosts[i].gameObject.AddComponent<GhostFlock>();
            gf.parentBoid = this;
        }
    }
    void PositionGhosts()
    {
        var ghostPosition = transform.position;

        //left
        ghostPosition.x = transform.position.x - screenWidth;
        ghostPosition.y = transform.position.y;
        ghosts[0].position = ghostPosition;
        ghosts[0].rotation = transform.rotation;


        //right
        ghostPosition.x = transform.position.x + screenWidth;
        ghostPosition.y = transform.position.y;
        ghosts[1].position = ghostPosition;
        ghosts[1].rotation = transform.rotation;
    }

    void SwapShips()
    {
        foreach (var ghost in ghosts)
        {

            if (ghost.position.x < (screenWidth / 2) && ghost.position.x > -1 * (screenWidth / 2))
            {
                Debug.Log("swapping with ghost");
                transform.position = ghost.position;
                break;
            }
        }
        PositionGhosts();
    }

    private void Flock(IEnumerable<Flocking> boids)
    {
        var alignment = Alignment(boids);
        var separation = Separation(boids);
        var cohesion = Cohesion(boids);
        var guide = Guide();

        acceleration = alignmentAmount * alignment + cohesionAmount * cohesion + separationAmount * separation + guideAmount * guide;
    }

    public void UpdateVelocity()
    {
        velocity += acceleration;
        velocity = LimitMagnitude(velocity, maxSpeed);
    }

    private void UpdatePosition()
    {
        Position += velocity * Time.deltaTime;
    }

    private void UpdateRotation()
    {
        var angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle) + baseRotation);
    }

    private Vector2 Guide()
    {
        Vector2 closestGuideVector = guideObjects[0].transform.position - transform.position;
        foreach (var guide in guideObjects)
        {
            Vector2 relGuide = guide.transform.position - transform.position;
            if(relGuide.magnitude < closestGuideVector.magnitude)
            {
                closestGuideVector = relGuide;
            }
        }

        if (closestGuideVector.magnitude < guideDistance)
        {
            Debug.DrawRay(transform.position, closestGuideVector, Color.green);
            var steer = Steer(closestGuideVector.normalized * maxSpeed);
            return steer;

        }
        else
        {
            return new Vector2(0, 0);
        }
    }

    private Vector2 Alignment(IEnumerable<Flocking> boids)
    {
        var velocity = Vector2.zero;
        if (!boids.Any()) return velocity;

        foreach (var boid in boids)
        {
            velocity += boid.velocity;
        }
        velocity /= boids.Count();

        var steer = Steer(velocity.normalized * maxSpeed);
        return steer;
    }

    private Vector2 Cohesion(IEnumerable<Flocking> boids)
    {
        if (!boids.Any()) return Vector2.zero;

        var sumPositions = Vector2.zero;
        foreach (var boid in boids)
        {
            sumPositions += boid.Position;
        }
        var average = sumPositions / boids.Count();
        var direction = average - Position;

        var steer = Steer(direction.normalized * maxSpeed);
        return steer;
    }

    private Vector2 Separation(IEnumerable<Flocking> boids)
    {
        var direction = Vector2.zero;
        boids = boids.Where(o => DistanceTo(o) <= neighborhoodRadius / 2);
        if (!boids.Any()) return direction;

        foreach (var boid in boids)
        {
            var difference = Position - boid.Position;
            direction += difference.normalized / difference.magnitude;
        }
        direction /= boids.Count();

        var steer = Steer(direction.normalized * maxSpeed);
        return steer;
    }

    private Vector2 Steer(Vector2 desired)
    {
        var steer = desired - velocity;
        steer = LimitMagnitude(steer, maxForce);

        return steer;
    }

    private float DistanceTo(Flocking boid)
    {
        return Vector3.Distance(boid.transform.position, Position);
    }

    private Vector2 LimitMagnitude(Vector2 baseVector, float maxMagnitude)
    {
        if (baseVector.sqrMagnitude > maxMagnitude * maxMagnitude)
        {
            baseVector = baseVector.normalized * maxMagnitude;
        }
        return baseVector;
    }

    private void WrapAround()
    {
        if (Position.x < -14) Position = new Vector2(14, Position.y);
        if (Position.y < -8) Position = new Vector2(Position.x, 8);
        if (Position.x > 14) Position = new Vector2(-14, Position.y);
        if (Position.y > 8) Position = new Vector2(Position.x, -8);
    }
}
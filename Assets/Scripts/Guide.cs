using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class Guide : MonoBehaviour
{
    [Range(1, 5)]
    public float speed = 1f;

    public PathCreator pathCreator;
    float distanceTravelled;

    Transform[] ghosts = new Transform[2];

    public float screenWidth;
    public float screenHeight;
    // Start is called before the first frame update
    void Start()
    {
        var cam = Camera.main;

        var screenBottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, transform.position.z));
        var screenTopRight = cam.ViewportToWorldPoint(new Vector3(1, 1, transform.position.z));

        screenWidth = screenTopRight.x - screenBottomLeft.x;
        screenHeight = screenTopRight.y - screenBottomLeft.y;
        CreateGhosts();
        Debug.Log("ghost left:" + ghosts[0].position);
        Debug.Log("ghost right:" + ghosts[1].position);
        Debug.Log("main trans:" + transform.position);
        Debug.Log("width:" + screenWidth);
    }

    void CreateGhosts()
    {
        for(int i = 0; i < 2; i++)
        {
            ghosts[i] = Instantiate(transform, Vector3.zero, Quaternion.identity) as Transform;

            DestroyImmediate(ghosts[i].GetComponent<Guide>());
        }
        PositionGhosts();
    }

    void PositionGhosts()
    {
        var ghostPosition = transform.position;

        //left
        ghostPosition.x = transform.position.x - screenWidth;
        ghostPosition.y = transform.position.y;
        ghosts[0].position = ghostPosition;

        //right
        ghostPosition.x = transform.position.x + screenWidth;
        ghostPosition.y = transform.position.y;
        ghosts[1].position = ghostPosition;

    }

    void SwapShips()
    {
        foreach(var ghost in ghosts)
        {

            if(ghost.position.x < (screenWidth / 2) && ghost.position.x > -1 * (screenWidth/2))
            {
                Debug.Log("swapping with ghost");
                transform.position = ghost.position;
                break;
            }
        }
        PositionGhosts();
    }

    // Update is called once per frame
    void Update()
    {
        //distanceTravelled += speed * Time.deltaTime;
        //transform.position = pathCreator.path.GetPointAtDistance(distanceTravelled);

        //PositionGhosts();
        SwapShips();
    }

    private void FixedUpdate()
    {
        transform.position = new Vector3(transform.position.x + 0.07f, transform.position.y);
    }
}

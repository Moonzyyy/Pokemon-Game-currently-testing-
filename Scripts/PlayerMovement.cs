using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public AudioSource collisionErrorSound;
    //Change player speed
    public float moveSpeed;

    //Check if player is moving
    private bool isMoving;

    //check for player inputs
    private Vector2 input;

    //animation controller
    private Animator animator;

    //reference of layer
    public LayerMask solidObjectLayer;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    //Called every frame
    private void Update()
    {
        //This is called when not moving, always checking for any inputs
        if (!isMoving)
        {
            //get the 1 value inputs from the player
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            //need to change the way diagnal movement is stopped
            if (input.x !=0 )
            {
                input.y = 0;
            }

            //if a value is not 0
            
            if (input != Vector2.zero)
            {
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);
                //we will update the new target position
                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;

                if (isNotSolidObject(targetPos))
                {
                    //lets start the routine
                    StartCoroutine(Move(targetPos));
                }
            }
        }
        animator.SetBool("isMoving", isMoving);
    }

    //over a period of time
    //Ienumerator starts at start of input, therefore once input is 1 it ignores any other input changes until execution
    IEnumerator Move(Vector3 targetPos)
    {
        //player is moving
        isMoving = true;

        //checks if value is greather than a tiny value
        //dont get the while part
        while((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {   
            //move towards position slowly
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            //stop execution of courotine
            yield return null;
        }

        //player is not moving
        isMoving = false;
    }


    private bool isNotSolidObject(Vector3 targetPos)
    {
       if (Physics2D.OverlapCircle(targetPos, 0.1f, solidObjectLayer) != null)
        {
            collisionErrorSound.Play();
            return false;
        }
        
        return true;
    }
}

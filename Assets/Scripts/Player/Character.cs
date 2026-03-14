using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField]
    private Camera mainCamera;
    [SerializeField]
    private PlayerInput playerInput;
    [SerializeField]
    private PlayerMovement playerMovement;

    public float interactionRayLength = 5;

    public LayerMask groundMask;


    public bool fly = false;

    bool isWaiting = false;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        playerInput.OnMouseClick += HandleMouseClick;
        playerInput.OnFly += HandleFlyClick;
    }

    private void HandleFlyClick()
    {
        fly = !fly;
    }

    void Update()
    {
        if (fly)
        {
            playerMovement.Fly(playerInput.MovementInput, playerInput.IsJumping, playerInput.RunningPressed);

        }
        else
        {
            if (playerMovement.IsGrounded && playerInput.IsJumping && isWaiting == false)
            {
                isWaiting = true;
                StopAllCoroutines();
                StartCoroutine(ResetWaiting());
            }
            playerMovement.HandleGravity(playerInput.IsJumping);
            playerMovement.Walk(playerInput.MovementInput, playerInput.RunningPressed);


        }

    }
    IEnumerator ResetWaiting()
    {
        yield return new WaitForSeconds(0.1f);
        isWaiting = false;
    }

    private void HandleMouseClick()
    {


    }




}
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController characterController;
    private PlayerInput playerInput;
    private Animator animator;
    
    private Camera followCam;
    
    public float speed = 6f;
    public float jumpVelocity = 20f;
    [Range(0.01f, 1f)] public float airControlPercent;

    public float speedSmoothTime = 0.1f;
    public float turnSmoothTime = 0.1f;
    
    private float speedSmoothVelocity;
    private float turnSmoothVelocity;
    
    private float currentVelocityY;
    
    public float currentSpeed =>
        new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;
    
    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        followCam = Camera.main;
    }

    private void FixedUpdate()
    {
        // 플레이어가 속도를 내거나 총을 쏘는 보는 방향을 일치시킴
        if (currentSpeed > 0.2f || playerInput.fire) Rotate();

        Move(playerInput.moveInput);
        
        if (playerInput.jump) Jump();
    }

    private void Update()
    {
        UpdateAnimation(playerInput.moveInput);
    }

    public void Move(Vector2 moveInput)
    {
        var targetSpeed = speed * moveInput.magnitude;
        var moveDirection = Vector3.Normalize(transform.forward * moveInput.y + transform.right * moveInput.x);

        var smoothTime = characterController.isGrounded ? speedSmoothTime : speedSmoothTime / airControlPercent;

        //원래 값과 목표값 사이에 스무스한 값을 생성
        targetSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, smoothTime);

        //중력을 구현
        currentVelocityY += Time.deltaTime * Physics.gravity.y;//시간 * 중력가속도

        var velocity = moveDirection * targetSpeed + Vector3.up * currentVelocityY;

        characterController.Move(velocity * Time.deltaTime);

        if (characterController.isGrounded) currentVelocityY = 0f;
    }

    public void Rotate()
    {
        var targetRotation = followCam.transform.eulerAngles.y;

        targetRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime);

        transform.eulerAngles = Vector3.up * targetRotation;
    }

    public void Jump()
    {
        if (characterController.isGrounded) return;

        currentVelocityY = jumpVelocity;
    }

    private void UpdateAnimation(Vector2 moveInput)
    {
        var animationSpeedPercent = currentSpeed / speed;
        animator.SetFloat("Vertical Move", moveInput.y * animationSpeedPercent, 0.05f, Time.deltaTime);
        animator.SetFloat("Horizontal Move", moveInput.x * animationSpeedPercent, 0.05f, Time.deltaTime);
    }
}
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ___WorkData.Scripts.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        #region Animator Hashes
        public static readonly int Hash_MovementValue = Animator.StringToHash("MovementValue");
        #endregion

        #region Inspector Variables

        [SerializeField] private float walkingSpeed = 10f;
        [SerializeField] private float jumpSpeed = 5f;

        // Комбо-система
        [SerializeField] private int clickCount = 0;
        [SerializeField] private float clickTimer = 0f;
        [SerializeField] private float doubleClickTime = 0.1f;

        [SerializeField] private Animator animator;

        #endregion

        #region Input System Variables

        private InputSystem_Actions inputActions;

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction attackAction;

        private Vector2 moveInput;
        private bool lookingToTheRight = true;

        #endregion

        #region Cached Components

        private Rigidbody2D rb;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            inputActions = new InputSystem_Actions();

            moveAction = inputActions.Player.Move;
            jumpAction = inputActions.Player.Jump;
            attackAction = inputActions.Player.Attack;

            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            inputActions.Enable();

            moveAction.performed += Move;
            moveAction.canceled += Move;

            jumpAction.performed += OnJump;

            attackAction.performed += Attack;
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = new Vector2(moveInput.x * walkingSpeed, rb.linearVelocity.y);

            animator.SetFloat(Hash_MovementValue, Mathf.Abs(rb.linearVelocity.x));

            #region Combo Timer Logic
            if (clickTimer > 0)
            {
                clickTimer -= Time.deltaTime;

                if (clickTimer <= 0)
                {
                    clickCount = 0;
                }
            }
            #endregion
        }

        private void OnDisable()
        {
            moveAction.performed -= Move;
            moveAction.canceled -= Move;
            jumpAction.performed -= OnJump;
            attackAction.performed -= Attack;

            inputActions.Disable();
        }

        #endregion

        #region Input Callbacks

        private void Move(InputAction.CallbackContext ctx)
        {
            moveInput = ctx.ReadValue<Vector2>();

            if (moveInput.x > 0f)
                lookingToTheRight = true;
            else if (moveInput.x < 0f)
                lookingToTheRight = false;

            UpdateRotation();
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
        }

        private void Attack(InputAction.CallbackContext ctx)
        {
            clickCount = clickCount + 1;

            if (clickCount == 1)
            {
                animator.SetTrigger("ActionTrigger");
                animator.SetInteger("ActionID", 9);

                clickTimer = doubleClickTime;
            }
            else if (clickCount == 2)
            {
                animator.SetTrigger("ActionTrigger");
                animator.SetInteger("ActionID", 10);

                clickCount = 0;
                clickTimer = 0;
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateRotation()
        {
            if (lookingToTheRight)
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            else
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        #endregion
    }
}
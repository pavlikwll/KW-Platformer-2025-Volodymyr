using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ___WorkData.Scripts.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        #region ENUMS
        
        public enum PlayerActionState { Default, Attack, Attack_Dash }
       
        public enum PlayerMovementState { Idle, Run, Jump }
        public enum FacingDirection { Left, Right }
        [Header("Enum")] 
        public PlayerMovementState playerMovementState; 
        public PlayerActionState playerActionState;
        public FacingDirection facingDirection;
        
        #endregion
        
        #region ANIMATOR HASHES
        public static readonly int Hash_MovementValue = Animator.StringToHash("MovementValue");
        public static readonly int Hash_ActionID = Animator.StringToHash("ActionID");
        public static readonly int Hash_ActionTrigger = Animator.StringToHash("ActionTrigger");
        public static readonly int Hash_Grounded = Animator.StringToHash("Grounded");
        #endregion
        
        #region INSPECTOR VARIABLES

        [Header("Movement")]
        [SerializeField] private float walkingSpeed = 10f;
        [SerializeField] private float jumpForce = 5f;

        [Header("Combo System")]
        [SerializeField] private int clickCount = 0;
        [SerializeField] private float clickTimer = 0f;
        [SerializeField] private float doubleClickTime = 0.25f;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Ledge Grab")]
        [SerializeField] private Transform wallcheck;
        [SerializeField] private Transform groundcheck;
        [SerializeField] private LayerMask groundlayer;
        [SerializeField] private bool Grounded;

        private bool isGrabbing = false;
        private Vector2 direction;

        #endregion
        
        #region INPUT SYSTEM VARIABLES
        private void InputStuff()
        {
            
        }
        private InputSystem_Actions inputActions;

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction attackAction;

        private Vector2 moveInput;
        private bool lookingToTheRight = true;
        
        #endregion
        
        #region CACHED COMPONENTS
        private Rigidbody2D rb;
        #endregion
        
        #region UNITY LIFECYCLE
        
        // ПРИЧИНА:
        //   Налаштовуємо систему вводу та беремо компоненти Unity.
        // МЕХАНІКА:
        //   Прив'язуємо InputActions до локальних змінних.
        // НАСЛІДОК:
        //   Скрипт знає, які кнопки відповідають за рух/стрибок/атаку.
        private void Awake()
        {
            inputActions = new InputSystem_Actions();

            playerActionState = PlayerActionState.Default;

            moveAction = inputActions.Player.Move;
            jumpAction = inputActions.Player.Jump;
            attackAction = inputActions.Player.Attack;

            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            
            if (facingDirection == FacingDirection.Right)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }

        // ПРИЧИНА:
        //   Потрібно почати отримувати сигнали від Input System.
        // МЕХАНІКА:
        //   Підписуємось на події: Move, Jump, Attack.
        // НАСЛІДОК:
        //   Коли гравець натисне кнопку — викликається потрібний метод.
        private void OnEnable()
        {
            inputActions.Enable();

            moveAction.performed += Move;
            moveAction.canceled  += Move;

            jumpAction.performed += OnJump;

            attackAction.performed += Attack;
        }

        // ПРИЧИНА:
        //   Усі фізичні дії мають запускатись у стабільному FixedUpdate.
        // МЕХАНІКА:
        //   Викликаємо три основні логічні блоки.
        // НАСЛІДОК:
        //   Персонаж рухається, робить комбо, чіпляється за край.
        private void FixedUpdate()
        {
            Grounded = Physics2D.Raycast(groundcheck.position, Vector2.down, 0.2f, groundlayer);
            
            HandleMovement();
            HandleComboTimer();
            HandleLedgeGrab();
        }

        // Вимикаємо всі підписки, щоб уникнути помилок
        private void OnDisable()
        {
            moveAction.performed -= Move;
            moveAction.canceled  -= Move;
            jumpAction.performed -= OnJump;
            attackAction.performed -= Attack;

            inputActions.Disable();
        }

        #endregion
        
        #region MOVEMENT LOGIC

        // ЛОГІКА РУХУ
        // ПРИЧИНА:
        //   Гравець повинен рухатися вліво-вправо.
        //
        // УМОВА:
        //   Якщо він ЧІПЛЯЄТЬСЯ за стіну (isGrabbing = true),
        //   рух повністю блокується.
        //
        // МЕХАНІКА:
        //   Беремо moveInput.x (–1 / 0 / 1),
        //   множимо на walkingSpeed,
        //   задаємо Rigidbody швидкість по X.
        //
        // НАСЛІДОК:
        //   Гравець рухається плавно, а Animator знає швидкість.
        private void HandleMovement()
        {
            if (!isGrabbing)
            {
                rb.linearVelocity = new Vector2(moveInput.x * walkingSpeed, rb.linearVelocity.y);
            }

            animator.SetFloat(Hash_MovementValue, Mathf.Abs(rb.linearVelocity.x));
        }

        #endregion
        
        #region LEDGE GRAB LOGIC

        // ЛОГІКА ЗАХВАТУ ЗА КРАЙ
        // ПРИЧИНА:
        //   Коли персонаж ковзає вниз по стіні — він має зупинитися й «виснути».
        //
        // УМОВА:
        //   — НЕ стоїть на землі
        //   — Є стіна перед ним
        //   — Він падає (швидкість по Y < 0)
        //
        // МЕХАНІКА:
        //   Вимикаємо гравітацію → персонаж не падає.
        //   Обнуляємо швидкість → персонаж висить на місці.
        //   Вмикаємо ActionID=20 → анімація хвату.
        //
        // НАСЛІДОК:
        //   Персонаж зависає на краю і чекає на команду ↑ або ↓.
        private void HandleLedgeGrab()
        {
            direction = lookingToTheRight ? Vector2.right : Vector2.left;

            
            bool TouchingWall = Physics2D.Raycast(wallcheck.position, direction, 0.2f, groundlayer);

            // Вхід у хват
            if (!Grounded && TouchingWall && rb.linearVelocity.y < 0)
            {
                rb.gravityScale = 0;
                rb.linearVelocity = Vector2.zero;
                isGrabbing = true;
            }

            // Відображення анімації хвату
            if (isGrabbing)
            {
                animator.SetInteger(Hash_ActionID, 20);
            }
            
            if (isGrabbing && moveInput.y > 0) // Вихід вгору
            {
                rb.gravityScale = 5;
                isGrabbing = false;
            }
            else if (isGrabbing && moveInput.y < 0) // Вихід вниз
            {
                rb.gravityScale = 5;
                isGrabbing = false;
            }
        }

        #endregion
        
        #region COMBO SYSTEM LOGIC

        // ЛОГІКА КОМБО-АТАК
        // ПРИЧИНА:
        //   Потрібно виконати дві атаки поспіль.
        //
        // УМОВА:
        //   Перша атака запускає таймер.
        //   Друга атака повинна бути ДО того, як він завершиться.
        //
        // МЕХАНІКА:
        //   Зменшуємо clickTimer щофрейма.
        //   Якщо час закінчився → clickCount обнуляється.
        //
        // НАСЛІДОК:
        //   Система визначає: це перший удар чи комбо.
        private void HandleComboTimer()
        {
            if (clickTimer > 0)
            {
                clickTimer = clickTimer - Time.deltaTime;

                // Коли таймер закінчився → комбо скасовується
                if (clickTimer <= 0)
                {
                    clickCount = 0;
                }
            }
        }

        #endregion
        
        #region INPUT CALLBACKS

        // РУХ (Move)
        // ПРИЧИНА:
        //   Коли гравець натискає A/D або ←/→ — ми маємо прочитати це значення.
        //
        // УМОВА:
        //   ctx містить вектор руху (X/Y), зчитуємо тільки X.
        //
        // МЕХАНІКА:
        //   Оновлюємо moveInput,
        //   визначаємо, куди дивиться персонаж,
        //   робимо поворот трансформа.
        //
        // НАСЛІДОК:
        //   Персонаж дивиться у правильний бік під час руху.
        private void Move(InputAction.CallbackContext ctx)
        {
            moveInput = ctx.ReadValue<Vector2>();

            if (moveInput.x > 0f)
            {
                lookingToTheRight = true;
            }
            else if (moveInput.x < 0f)
            {
                lookingToTheRight = false;
            }

            if (moveInput.x == 0)
            {
                playerMovementState = PlayerMovementState.Idle;
            }
            else
            {
                playerMovementState = PlayerMovementState.Run;
            }

            if (moveInput.x > 0f)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                facingDirection = FacingDirection.Right;
            }
            else if(moveInput.x < 0)
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
                facingDirection = FacingDirection.Left;
            }
        }
        
        // СТРИБОК (Jump)
        // ПРИЧИНА:
        //   Гравець повинен мати вертикальний рух угору.
        //
        // УМОВА:
        //   Кнопка реально спрацювала (performed).
        //
        // МЕХАНІКА:
        //   Даємо нову швидкість по Y = jumpForce.
        //   Вмикаємо тригер анімації.
        //
        // НАСЛІДОК:
        //   Персонаж починає стрибок.
        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            if (!Grounded) return;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            animator.SetTrigger(Hash_ActionTrigger);
            animator.SetInteger(Hash_ActionID, 1);

            UpdateAnimator();
        }


        // АТАКА (Attack)
        // ПРИЧИНА:
        //   Потрібно розрізняти «звичайний удар» і «комбо-удар».
        //
        // УМОВА:
        //   Перший клік → Attack 1.
        //   Другий клік ДО завершення таймера → Attack 2.
        //
        // МЕХАНІКА:
        //   Підвищуємо clickCount,
        //   запускаємо таймер,
        //   перемикаємо ActionID.
        //
        // НАСЛІДОК:
        //   Система анімацій знає, яку атаку запускати.
        private void Attack(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;

            if (playerActionState == PlayerActionState.Attack)
            {
                return;
            }
            
            clickCount = clickCount + 1;

            if (clickCount == 1)
            {
                animator.SetTrigger(Hash_ActionTrigger);
                animator.SetInteger(Hash_ActionID, 9);
                clickTimer = doubleClickTime;
            }
            else if (clickCount == 2)
            {
                animator.SetTrigger(Hash_ActionTrigger);
                animator.SetInteger(Hash_ActionID, 10);
                clickTimer = 0;
                clickCount = 0;
            }
        }

        #endregion
        
        #region ANIMATION HELPERS

        // ОНОВЛЕННЯ АНІМАТОРА
        // ПРИЧИНА:
        //   Animator повинен знати швидкість і стан землі.
        //
        // НАСЛІДОК:
        //   Можна коректно перемикати анімації Idle ↔ Run ↔ Jump.
        private void UpdateAnimator()
        {
            animator.SetFloat(Hash_MovementValue, rb.linearVelocity.magnitude);
            animator.SetBool(Hash_Grounded, Grounded);
        }


        // ПОВОРОТ ПЕРСОНАЖА
        // ПРИЧИНА:
        //   Анімації повинні дивитися у сторону руху.
        //
        // МЕХАНІКА:
        //   Повертаємо персонажа по Y на 0° або 180°.
        //
        // НАСЛІДОК:
        //   Персонаж завжди “дивиться” у правильний бік.
        private void UpdateRotation()
        {
            
        }

        #endregion
    }
}
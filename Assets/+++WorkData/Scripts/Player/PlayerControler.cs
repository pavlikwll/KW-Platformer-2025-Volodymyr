// ⬇ Підключаємо базові простори назв .NET і Unity.
using System;
using Unity.Collections; // Дає змогу працювати зі стандартними типами .NET і винятками.
using UnityEngine;                  // Головна бібліотека Unity: MonoBehaviour, GameObject, Transform, тощо.
using UnityEngine.InputSystem;      // Нова Input System (клавіатура/миша/ґеймпад). Без неї не працюватимуть InputAction-и.

namespace ___WorkData.Scripts.Player   // ⬅ Простір назв: логічна папка для коду (уникнення конфліктів імен).
{
    // Компонент Unity. Його можна прикріпити до об’єкта гравця у сцені.
    // Атрибут гарантує, що на об’єкті є Rigidbody2D (інакше Unity його підставить).
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        #region Inspector Variables
        // ⬇ Параметри, які видно в Inspector. Їх зручно крутити без перекомпіляції коду.

        [SerializeField] private float walkingSpeed = 5f;  // Базова швидкість ходьби (одиниці/сек у фізичному сенсі).
        [SerializeField] private float runningSpeed = 10f; // Запасом для бігу. Поки не використовуємо, але залишимо для майбутнього.
        #endregion

        #region Private Variables
        // ⬇ Робочі змінні — не видно в Inspector. Обслуговують логіку контролера.
        
        // Це «обгортка», яку генерує нова Input System з твого .inputactions-файла.
        // Вона містить мапінг усіх дій (Move/Jump/...).
        private InputSystem_Actions _inputActions;

        // Окремі посилання на конкретні дії вводу з мапи «Player».
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _attackAction;
        private InputAction _lookAction;
        private InputAction _interactAction;
        private InputAction _crouchAction;
        private InputAction _previousAction;
        private InputAction _nextAction;
        private InputAction _rollAction;

        // Тут зберігаємо останній зчитаний напрям руху (X,Y) із Input System.
        // Для платформера нас цікавить переважно X: -1 (ліво), 0 (стоп), 1 (право).
        private Vector2 _moveInput;

        // Посилання на фізичне тіло 2D. Через нього ми «рухаємо» об’єкт, змінюючи швидкість.
        private Rigidbody2D rb;
        private SpriteRenderer sr;
        #endregion

        // Життєвий цикл Unity: послідовність подій від створення компонента до його вимкнення.
        // Awake() -> OnEnable() -> (Update/FixedUpdate кожен кадр) -> OnDisable() -> OnDestroy()

        private void Awake()
        {
            // 1) Створюємо екземпляр згенерованого класу дій.
            _inputActions = new InputSystem_Actions();

            // 2) Дістаємо конкретні дії з мапи «Player».
            //    Тепер _moveAction знає, з яких клавіш/стика брати вхідні дані.
            _moveAction = _inputActions.Player.Move;
            _jumpAction = _inputActions.Player.Jump;
            _sprintAction = _inputActions.Player.Sprint;
            _attackAction = _inputActions.Player.Attack;
            _lookAction = _inputActions.Player.Look;
            _interactAction = _inputActions.Player.Interact;
            _crouchAction = _inputActions.Player.Crouch;
            _previousAction = _inputActions.Player.Previous;
            _nextAction = _inputActions.Player.Next;
            _rollAction = _inputActions.Player.Roll;

            // 3) Беремо посилання на Rigidbody2D на цьому ж об’єкті.
            //    Через нього будемо виставляти швидкість у фізиці.
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();

            sr.flipX = true;

        }

        private void OnEnable()
        {
            // Активуємо всю Input-систему для цього контролера.
            _inputActions.Enable();

            // Підписуємося на події руху:
            // performed — коли є активний вхід (тиснемо A/D або рухаємо стік),
            // canceled  — коли вхід зник (клавішу відпустили) → отримаємо (0,0).
            _moveAction.performed += Move;
            _moveAction.canceled  += Move;
        }
        
        // FixedUpdate() викликається через рівні інтервали часу (за замовчуванням 0,02 с).
        // Усе, що стосується фізики (Rigidbody2D), коректно робити саме тут.
        
        private void FixedUpdate()
        {
            // Беремо X з інпуту (-1..1), множимо на швидкість, отримуємо потрібну горизонтальну швидкість.
            // По Y нічого не змінюємо — залишаємо поточну («гравітація/стрибок» працює як є).
            rb.linearVelocity = new Vector2(_moveInput.x * walkingSpeed, rb.linearVelocity.y);

            // ВАЖЛИВО: тут не має бути throw NotImplementedException(); — це штучний краш для заглушок.

            rb.linearVelocityX = _moveInput.x * walkingSpeed;
        }
        
        private void OnDisable()
        {
            // Відписуємося від подій, інакше після вимкнення об’єкта підписки «висять у повітрі».
            _moveAction.performed -= Move;
            _moveAction.canceled  -= Move;

            // Вимикаємо Input-систему для охайности.
            _inputActions.Disable();
        }

        #region Input
        // Колбек для подій _moveAction. Викликається і на натисканні, і на відпусканні.
        private void Move(InputAction.CallbackContext ctx)
        {
            // Читаємо значення як Vector2. Для 2D платформера це зазвичай:
            // A/D або ←/→ → дають (-1,0) / (1,0). Джойстик — будь-яке число між ними.
            _moveInput = ctx.ReadValue<Vector2>();

            if (_moveInput.x > 0)
            {
                
            }
            
        }
        #endregion
    }
}
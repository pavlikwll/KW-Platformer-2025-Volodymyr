// Підключаємо основну Unity бібліотеку, 
// щоб мати доступ до базових класів, як MonoBehaviour, GameObject, Transform тощо.
using UnityEngine;

// Підключаємо нову Input System — це сучасна система для обробки керування
// (клавіатура, мишка, геймпад, тощо). Без цього не працюватимуть InputAction-и.
using UnityEngine.InputSystem;

// Оголошення класу PlayerController, який є компонентом Unity (MonoBehaviour).
// Його можна прикріпити до будь-якого GameObject у сцені, наприклад — до гравця.
public class PlayerController : MonoBehaviour
{
    // 🔹 Цей блок змінних видно в Unity Inspector — для налаштування вручну.
    #region Inspector Variables

    // [SerializeField] робить приватну змінну доступною в інспекторі Unity.
    // walkingSpeed — швидкість, з якою гравець ходить.
    [SerializeField]
    private float walkingSpeed;

    #endregion

    // 🔹 Приватні змінні — для внутрішньої логіки контролера.
    // Їх не видно у Unity, але вони зберігають важливі посилання та дані.
    #region Private Variables

    // Посилання на InputSystem_Actions — це клас, який генерується автоматично
    // на основі твого Input Actions Asset (файлу з усіма кнопками і командами).
    private InputSystem_Actions _inputActions;

    // Окремі InputAction-и для кожної дії гравця.
    // Вони відповідають за рух, стрибок, атаку тощо.
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

    // Vector2 зберігає введення руху (ось X і Y).
    // Наприклад: (1,0) = рух праворуч, (-1,0) = ліворуч.
    private Vector2 _moveInput;

    #endregion

    // 🔹 Події життєвого циклу Unity (Unity Event Functions)
    // Це спеціальні методи, які викликаються автоматично.
    #region Unity Event Functions

    // Awake() викликається найпершим, коли створюється об’єкт.
    // Тут ініціалізуємо InputSystem і підключаємо дії з Input Asset.
    private void Awake()
    {
        // Створюємо новий екземпляр класу InputSystem_Actions.
        _inputActions = new InputSystem_Actions();

        // Прив’язуємо всі дії з розділу “Player” до локальних змінних.
        // Це дозволяє легко керувати кожною кнопкою окремо.
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
    }

    // OnEnable() викликається, коли об’єкт активується (enabled = true).
    // Тут ми “вмикаємо” систему вводу та підписуємося на події кнопок.
    private void OnEnable()
    {
        // Активуємо весь набір InputAction-ів.
        _inputActions.Enable();

        // Підписуємо метод Move() на події: 
        // - performed (коли кнопка натиснута або рух почато)
        // - canceled (коли рух припинено).
        _moveAction.performed += Move;
        _moveAction.canceled += Move;
    }

    // OnDisable() викликається, коли об’єкт деактивується (disabled або знищується).
    // Тут ми вимикаємо InputSystem і знімаємо підписки, щоб уникнути помилок.
    private void OnDisable()
    {
        // Вимикаємо Input System, щоб вона не слухала події.
        _inputActions.Disable();

        // Відписуємо Move() від подій, щоб не залишилося “зайвих слухачів”.
        _moveAction.performed -= Move;
        _moveAction.canceled -= Move;
    }

    #endregion

    // 🔹 Вхідні події (обробка натискань і рухів)
    #region Input

    // Метод Move() викликається щоразу, коли користувач рухаєсь або відпускає кнопку руху.
    // ctx — контекст події, який містить усю інформацію про введення.
    private void Move(InputAction.CallbackContext ctx)
    {
        // Зчитуємо напрям руху у вигляді Vector2 (ось X і Y)
        // і зберігаємо в _moveInput для подальшого використання в Update().
        _moveInput = ctx.ReadValue<Vector2>();
    }

    #endregion
}
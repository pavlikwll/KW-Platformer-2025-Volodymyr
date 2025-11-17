using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ___WorkData.Scripts.Player
{
    // Цим атрибутом ти говориш Unity: "На цьому GameObject ОБОВ’ЯЗКОВО має бути Rigidbody2D".
    // Якщо його нема — Unity автоматично додасть його при додаванні цього скрипту.
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        // Попередньо рахуємо хеш параметра Animator "MovementValue".
        // Це робиться один раз і зберігається як статичне незмінне число (int).
        // Навіщо:
        // • уникаємо помилок у рядку ("MovementValue") завдяки одному надійному ID,
        // • працюємо значно швидше, ніж зі звичайними рядками,
        // • економимо пам’ять і виключаємо дублювання,
        // • маємо професійний спосіб звернення до параметрів Animator.
        // Тепер у SetFloat() достатньо викликати: SetFloat(Hash_MovementValue, value);
        // і Animator миттєво знайде потрібний параметр.
        public static readonly int Hash_MovementValue = Animator.StringToHash("MovementValue");

        #region Inspector Variables
        // ⬇ Параметри, які видно в Inspector. Їх зручно змінювати без редагування коду.
        // ВАЖЛИВО: без [SerializeField] Unity не покаже приватні поля в Inspector.

        [SerializeField] private float walkingSpeed = 5f;  // Швидкість горизонтального руху при звичайній ході.
        [SerializeField] private float jumpSpeed = 5f;     // Вертикальна швидкість при стрибку.
        
        // Посилання на Animator, який керує анімаціями персонажа.
        // Якщо не призначити його в Inspector, у Awake() ми спробуємо знайти компонент автоматично.
        [SerializeField] private Animator animator;
        #endregion

        #region Private Variables
        // ⬇ Робочі змінні — не видно в Inspector, але критично важливі для логіки.

        // Це клас, який згенерувала нова Input System з твого файлу .inputactions.
        // Він містить мапу "Player" з діями Move, Jump, Roll тощо.
        // Без цього об’єкта ти не зміг би звертатися до _inputActions.Player.Move і т. ін.
        private InputSystem_Actions _inputActions;

        // Окремі "ручки" на дії з мапи Player.
        // _moveAction відповідає за рух (вісь/стрілки/стік),
        // _jumpAction — за стрибок,
        // _rollAction — за перекат.
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _attackAction;

        // Тут зберігається останнє прочитане значення руху з Input System.
        // Move зазвичай повертає (-1,0) / (1,0) або будь-яке число між ними для X.
        // Це значення ти потім використовуєш у FixedUpdate для руху Rigidbody2D.
        private Vector2 _moveInput;

        // Посилання на Rigidbody2D поточного об’єкта.
        // Через нього ти насправді "рухаєш" гравця, змінюючи його швидкість.
        // Без цього посилання вся фізика (рух, стрибок, перекат) не працювала б.
        private Rigidbody2D _rb;
        
        // Цей прапорець зберігає, куди зараз "дивиться" гравець:
        // true  → праворуч,
        // false → ліворуч.
        // На основі цього прапорця UpdateRotation обертає персонажа.
        private bool _lookingToTheRight = true;

        #endregion
        
        #region Unity Event Functions
        
        // Життєвий цикл Unity (основні моменти, які стосуються цього скрипту):
        // Awake()       → викликається при створенні компонента (до Start).
        // OnEnable()    → коли об’єкт/компонент стає активним.
        // FixedUpdate() → кожен фіксований крок фізики.
        // OnDisable()   → коли об’єкт/компонент вимикають.
        // OnDestroy()   → коли об’єкт видаляють.

        private void Awake()
        {
            // 1) Створюємо екземпляр згенерованого класу InputSystem_Actions.
            //    Тут ти ініціалізуєш всю систему вводу для цього скрипту.
            _inputActions = new InputSystem_Actions();

            // 2) Дістаємо конкретні дії з мапи "Player".
            //    Тепер _moveAction, _jumpAction, _rollAction знають, на які клавіші/стік вони прив’язані.
            _moveAction = _inputActions.Player.Move;
            _jumpAction = _inputActions.Player.Jump;
            _attackAction = _inputActions.Player.Attack;

            // 3) Отримуємо посилання на Rigidbody2D на цьому ж GameObject.
            //    Без цього _rb буде null, і при зверненні до _rb.linearVelocity буде помилка.
            _rb = GetComponent<Rigidbody2D>();

            // 4) Якщо Animator не призначено в Inspector, пробуємо знайти його на цьому ж GameObject.
            //    Це підстрахування, щоб уникнути NullReferenceException.
            animator = GetComponent<Animator>();
            
        }

        private void OnEnable()
        {
            // Увімкнути всю систему вводу для цього об’єкта.
            // Без цього InputAction-и не працюватимуть (події не надходитимуть).
            _inputActions.Enable();

            // Підписка на події руху:
            // performed — коли є активний вхід (клавіша натиснута/стік відхилений),
            // canceled  — коли вхід скинувся (клавішу відпущено, стік повернувся в 0).
            // Обидва випадки ведуть у метод Move, який оновлює _moveInput.
            _moveAction.performed += Move;
            _moveAction.canceled  += Move;

            // Підписка на стрибок:
            // коли дія Jump спрацьовує (кнопку натиснуто) — викликається OnJump.
            _jumpAction.performed += OnJump;

            // Підписка на перекат:
            // коли дія Roll спрацьовує — викликається OnRoll.
            _attackAction.performed += Attack;
        }
        
        // FixedUpdate() — спеціальний метод для фізики.
        // Він викликається з фіксованим кроком часу (наприклад, кожні 0.02 с),
        // тому саме сюди правильно ставити зміну швидкості Rigidbody2D.
        private void FixedUpdate()
        {
            // Беремо X із _moveInput (-1..1), помножуємо на walkingSpeed.
            // Це дає нам цільову горизонтальну швидкість.
            // По Y швидкість не змінюємо: там уже працює гравітація і стрибок.
            _rb.linearVelocity = new Vector2(_moveInput.x * walkingSpeed, _rb.linearVelocity.y);
            
            // Оновлюємо параметр аніматора "MovementValue":
            // Hash_MovementValue — хешоване ім’я параметра в Animator,
            // Mathf.Abs(...) — беремо модуль горизонтальної швидкости,
            // _rb.linearVelocity.x — поточна швидкість руху по осі X.
            // Це дозволяє Animator перемикати анімації залежно від інтенсивности руху.
            animator.SetFloat(Hash_MovementValue, Mathf.Abs(_rb.linearVelocity.x));

            // ВАЖЛИВО:
            // _moveInput оновлюється в методі Move(), який викликається через події InputAction.
            // Якщо Move() ніколи не викличеться (немає вводу / не підписався в OnEnable),
            // то _moveInput залишиться (0,0), і гравець не рухатиметься.
            
        }
        
        private void OnDisable()
        {
            // Відписуємося від усіх подій, інакше після вимкнення об’єкта
            // делегати можуть залишатися підписаними й ловити події, що призведе до помилок.
            _moveAction.performed -= Move;
            _moveAction.canceled  -= Move;
            _jumpAction.performed -= OnJump;
            _attackAction.performed -= Attack;

            // Вимикаємо InputSystem для охайности.
            _inputActions.Disable();
        }

        #endregion
        
        #region Input Methods
        // Move — це колбек для _moveAction.
        // Він викликається:
        // - коли ти починаєш рух (натиснув клавішу / відхилив стік),
        // - коли змінюється напрямок або сила вводу,
        // - коли відпускаєш клавішу (canceled).
        private void Move(InputAction.CallbackContext ctx)
        {
            // Читаємо значення інпуту як Vector2.
            // Для 2D-платформера тебе цікавить X:
            // -1 → ліворуч, 0 → стоїмо, 1 → праворуч
            // (а також проміжні значення при використанні геймпада).
            _moveInput = ctx.ReadValue<Vector2>();

            // На основі знака X визначаємо, куди дивитися:
            // > 0 → дивимось праворуч,
            // < 0 → дивимось ліворуч.
            // Якщо X == 0, прапорець не змінюється (залишається останній напрямок).
            if (_moveInput.x > 0f)
            {
                _lookingToTheRight = true;
            }
            else if (_moveInput.x < 0f)
            {
                _lookingToTheRight = false;
            }
            
            // Після оновлення прапорця напрямку — фізично обертаємо об’єкт.
            UpdateRotation();

        }
        
        // Цей метод фізично змінює поворот гравця в сцені.
        // Він дивиться на _lookingToTheRight і відповідно встановлює rotation:
        // (0, 0, 0)   — "дивимось" праворуч,
        // (0, 180, 0) — "дивимось" ліворуч (дзеркально по осі Y).
        private void UpdateRotation()
        {
            if (_lookingToTheRight)
            {
                // Нормальний стан — дивимося праворуч.
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            else
            {
                // Поворот на 180° навколо осі Y — спрайт/об’єкт дивиться ліворуч.
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        // Обробка стрибка.
        // Цей метод викликається, коли дія Jump надсилає performed:
        // наприклад, коли ти натискаєш пробіл або кнопку, прив’язану до Jump.
        private void OnJump(InputAction.CallbackContext ctx)
        {
            // Переконуємося, що це саме момент "спрацювання" (а не інші стани).
            if (!ctx.performed) return;

            // Задаємо нову вертикальну швидкість:
            // X залишаємо як є (щоб не зламати горизонтальний рух),
            // Y ставимо jumpSpeed — тобто стрибок угору.
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpSpeed);

            // ВАЖЛИВО:
            // Тут немає перевірки, чи гравець стоїть на землі —
            // отже стрибок можливий і в повітрі (множинні стрибки).
        }

        private void Attack(InputAction.CallbackContext ctx)
        {
            animator.SetTrigger("ActionTrigger");
            animator.SetInteger("ActionID", 9);
        }

        // Обробка перекату.
        // Викликається, коли дія Roll надсилає performed.
        #endregion
        
    }
}
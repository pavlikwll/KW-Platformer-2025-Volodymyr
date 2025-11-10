using UnityEngine;
using UnityEngine.InputSystem;

namespace ___WorkData.Scripts.Player
{
    // Цим атрибутом ти говориш Unity: "На цьому GameObject ОБОВ’ЯЗКОВО має бути Rigidbody2D".
    // Якщо його нема — Unity автоматично додасть при додаванні цього скрипту.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        #region Inspector Variables
        // ⬇ Параметри, які видно в Inspector. Їх зручно міняти без зміни коду.
        // ВАЖЛИВО: без [SerializeField] Unity не покаже приватні поля в Inspector.

        [SerializeField] private float walkingSpeed = 5f;  // Швидкість горизонтального руху при звичайній ходьбі.
        [SerializeField] private float runningSpeed = 10f; // Потенційна швидкість бігу (зараз не використовується, але може стати в пригоді).
        [SerializeField] private float jumpSpeed = 5f;     // Швидкість (вгору) при стрибку.
        [SerializeField] private float rollSpeed = 5f;     // Швидкість (вбік) при перекаті.
        #endregion

        #region Private Variables
        // ⬇ Робочі змінні — не видно в Inspector, але критично важливі для логіки.

        // Це клас, який згенерувала нова Input System з твого файлу .inputactions.
        // Він містить мапу "Player" з діями Move, Jump, Roll тощо.
        // Без цього об’єкта ти не зміг би звертатися до _inputActions.Player.Move і т.д.
        private InputSystem_Actions _inputActions;

        // Окремі "ручки" на дії з мапи Player.
        // _moveAction відповідає за рух (вісь/стрілки/стик),
        // _jumpAction — за стрибок,
        // _rollAction — за перекат.
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _rollAction;

        // Тут зберігається останнє прочитане значення руху з Input System.
        // Move зазвичай повертає (-1,0) / (1,0) або будь-яке число між ними для X.
        // Це значення ти потім використовуєш у FixedUpdate для руху Rigidbody2D.
        private Vector2 _moveInput;

        // Посилання на Rigidbody2D поточного об’єкта.
        // Через нього ти насправді "рухаєш" гравця, змінюючи його швидкість.
        // Без цього посилання вся фізика (рух, стрибок, перекат) не працювала б.
        private Rigidbody2D _rb;

        // Посилання на SpriteRenderer (зараз не використовується, але ти міг би через нього міняти спрайт/анімації/колір).
        private SpriteRenderer _sr;
        
        // Цей прапорець зберігає, куди зараз "дивиться" гравець:
        // true  → праворуч,
        // false → ліворуч.
        // На основі цього прапорця UpdateRotation обертає персонажа.
        private bool _lookingToTheRight = true;
        #endregion

        // Життєвий цикл Unity:
        // Awake()       → викликається при створенні компонента (до Start).
        // OnEnable()    → коли об’єкт/компонент стає активним.
        // FixedUpdate() → кожен фіксований крок фізики.
        // OnDisable()   → коли об’єкт/компонент вимикають.
        // OnDestroy()   → коли об’єкт видаляється.

        private void Awake()
        {
            // 1) Створюємо екземпляр згенерованого класу InputSystem_Actions.
            //    Тут ти ініціалізуєш всю систему вводу для цього скрипту.
            _inputActions = new InputSystem_Actions();

            // 2) Дістаємо конкретні дії з мапи "Player".
            //    Тепер _moveAction, _jumpAction, _rollAction знають, на які клавіші/стік підписані.
            _moveAction = _inputActions.Player.Move;
            _jumpAction = _inputActions.Player.Jump;
            _rollAction = _inputActions.Player.Roll;

            // 3) Отримуємо посилання на Rigidbody2D на цьому ж GameObject.
            //    Без цього _rb буде null, і при зверненні до _rb.linearVelocity буде помилка.
            _rb = GetComponent<Rigidbody2D>();

            // (Опціонально сюди можна було б додати первинну орієнтацію, якщо потрібно.)
        }

        private void OnEnable()
        {
            // Увімкнути всю систему вводу для цього об’єкта.
            // Без цього InputAction-и не працюватимуть (події не приходитимуть).
            _inputActions.Enable();

            // Підписка на події руху:
            // performed — коли є активний вхід (клавіша натиснена/стік відхилений),
            // canceled  — коли вхід скинувся (клавіша відпущена, стік повернувся в 0).
            // Обидва випадки ведуть у один метод Move, який оновлює _moveInput.
            _moveAction.performed += Move;
            _moveAction.canceled  += Move;

            // Підписка на стрибок:
            // коли дія Jump спрацьовує (кнопка натиснута) — викликається OnJump.
            _jumpAction.performed += OnJump;

            // Підписка на перекат:
            // коли дія Roll спрацьовує — викликається OnRoll.
            _rollAction.performed += OnRoll;
        }
        
        // FixedUpdate() — спеціальний метод для фізики.
        // Він викликається з фіксованим кроком часу (наприклад, кожні 0.02 сек),
        // тому саме сюди правильно ставити зміну швидкості Rigidbody2D.
        private void FixedUpdate()
        {
            // Беремо X із _moveInput (-1..1), помножуємо на walkingSpeed.
            // Це дає нам цільову горизонтальну швидкість.
            // По Y швидкість не змінюємо: там уже працює гравітація і стрибок.
            _rb.linearVelocity = new Vector2(_moveInput.x * walkingSpeed, _rb.linearVelocity.y);

            // ВАЖЛИВО:
            // _moveInput оновлюється в методі Move(), який викликається через події InputAction.
            // Якщо Move() ніколи не викличеться (нема вводу / не підписався в OnEnable),
            // то _moveInput залишиться (0,0), і гравець не рухатиметься.
        }
        
        private void OnDisable()
        {
            // Відписуємося від усіх подій, інакше після вимкнення об’єкта
            // делегати можуть "висіти" й ловити події, що призведе до помилок.
            _moveAction.performed -= Move;
            _moveAction.canceled  -= Move;
            _jumpAction.performed -= OnJump;
            _rollAction.performed -= OnRoll;

            // Вимикаємо InputSystem для охайности.
            _inputActions.Disable();
        }

        #region Input
        // Move — це колбек для _moveAction.
        // Він викликається:
        // - коли ти починаєш рух (нажал/відхилив стік),
        // - коли змінюється напрямок/сила,
        // - коли відпускаєш клавішу (canceled).
        private void Move(InputAction.CallbackContext ctx)
        {
            // Читаємо значення інпуту як Vector2.
            // Для 2D-платформера тебе цікаво X:
            // -1 → ліво, 0 → стоїмо, 1 → право (а також проміжні значення при геймпаді).
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
        // Він дивиться на _lookingToTheRight і відповідно ставить rotation:
        // (0, 0, 0)   — "дивимось" вправо,
        // (0, 180, 0) — "дивимось" вліво (дзеркально по осі Y).
        private void UpdateRotation()
        {
            if (_lookingToTheRight)
            {
                // Нормальний стан — дивимося вправо.
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            else
            {
                // Поворот на 180° навколо осі Y — спрайт/об’єкт виглядає вліво.
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        // Обробка стрибка.
        // Цей метод викликається, коли дія Jump надсилає performed:
        // наприклад, коли ти натискаєш пробіл або кнопку, прив’язану до Jump.
        private void OnJump(InputAction.CallbackContext ctx)
        {
            // Переконуємося, що це саме момент "спрацювання" (а не якісь інші стани).
            if (!ctx.performed) return;

            // Задаємо нову вертикальну швидкість:
            // X залишаємо як є (щоб не зламати горизонтальний рух),
            // Y ставимо jumpSpeed — тобто стрибок вгору.
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpSpeed);

            // ВАЖЛИВО:
            // Ти тут не перевіряєш, чи гравець на землі —
            // отже стрибок можливий і в повітрі (декілька разів).
        }
        
        // Обробка перекату.
        // Викликається, коли дія Roll надсилає performed.
        private void OnRoll(InputAction.CallbackContext ctx)
        {
            // Знову ж таки — реагуємо тільки на "performed", а не на інші стани.
            if (!ctx.performed) return;

            // Визначаємо напрямок перекату: знак від _moveInput.x.
            // Якщо ти тиснеш вправо → _moveInput.x > 0 → direction = +1.
            // Якщо вліво → _moveInput.x < 0 → direction = -1.
            // Якщо X == 0, Mathf.Sign поверне 0, і перекат буде "ніяк".
            float direction = Mathf.Sign(_moveInput.x);

            // Задаємо горизонтальну швидкість перекату:
            // X = direction * rollSpeed  → стрибок уліво/вправо,
            // Y залишаємо як є (щоб не зламати стрибок/падіння).
            _rb.linearVelocity = new Vector2(direction * rollSpeed, _rb.linearVelocity.y);

            // ВАЖЛИВО:
            // Тут немає таймера перекату або блокування керування.
            // Це одноразовий ривок: ти задав швидкість, далі фізика сама гальмує/рухає.
        }
        #endregion
    }
}
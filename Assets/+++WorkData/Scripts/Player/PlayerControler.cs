using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControler : MonoBehaviour
{
    #region Insepctor Variables

    [SerializeField]
    private float walkingSpeed;

    #endregion

    #region Private Variables

    private InputSystem_Actions _inputActions;
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

    public Vector2 _moveInput;

    #endregion

    #region Unity Event Functions
    /// <summary>
    ///  
    /// </summary>
    private void Awake()
    {
        _inputActions = new InputSystem_Actions();
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

    private void OnEnable()
    {
        _inputActions.Enable();
        _moveAction.performed += Move;
        _moveAction.canceled += Move;
    }
    private void OnDisable()
    {
        _inputActions.Enable();
        _moveAction.performed += Move;
        _moveAction.canceled += Move;
    }


    #endregion
    
    #region Input

    private void Move(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();

    }

    #endregion

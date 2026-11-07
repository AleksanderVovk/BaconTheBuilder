using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool OnlyRun;

    [SerializeField] private Transform _camera;
    [SerializeField] private Transform _playerCenter;

    [Header("Камера")]
    [SerializeField] private float _xRotation;
    [SerializeField] private float _yRotation;

    [Header("Гравитация")]
    public Vector3 velocity;

    [SerializeField] private float _grondYOffset;
    [SerializeField] private float _maxGravity;

    [SerializeField] Transform _groundCheck;
    [SerializeField] LayerMask _groundLayer;

    public int defaultJumpCount = 1;
    public int currentJumpCount;
    [SerializeField] private ParticleSystem _jumpParticle;

    [Header("Лестница")]
    [SerializeField] private Transform _ladderEye;
    [SerializeField] private LayerMask _ladderLayer;
    private bool _useLadder;
    private Transform _ladderTarget;

    [Header("Спрыгивание с лестницы назад")]
    [SerializeField] private bool enableJumpOffLadderBack = true;

    [Tooltip("Если true: спрыгивание только при 'назад + jump'. Если false: jump на лестнице всегда отлипает.")]
    [SerializeField] private bool requireBackAndJump = false;

    [SerializeField] private float ladderDetachTime = 0.25f;
    [SerializeField] private float ladderJumpBackForce = 4.0f;
    [SerializeField] private float ladderJumpUpForce = 3.0f;
    [SerializeField] private float ladderBackThreshold = -0.6f;
    private float _ladderDetachTimer;

    [Header("Падение и чекпоинты")]
    [SerializeField] private float _fallDeathHeight = 10f;
    private float _fallStartHeight;
    private bool _isFalling = false;
    private Vector3 _lastCheckpoint;

    [Header("Ссылки")]
    private InputsManager _inputsManager;
    private Player _player;
    private PlayerMovement _playerMovement;
    private PlayerState _playerState;
    private PlayerCharacteristic _playerCharacteristic;
    private WorldCharacteristic _worldCharacteristics;
    private CharacterController _controller;
    private PlayerJetpack _jetpack;

    [SerializeField] private AudioClip _jumpUpClip;
    [SerializeField] private AudioSource _audioSource;

    public System.Action OnFallDeath;

    [Header("Прыжок Coyote Time")]
    [SerializeField] private float _coyoteTime = 0.3f;
    private float _coyoteTimeCounter;
    private bool _wasGrounded;

    private void Awake()
    {
        InitializationComponents();
    }

    private void InitializationComponents()
    {
        try
        {
            _inputsManager = GetComponent<InputsManager>();
            _player = GetComponent<Player>();
            _playerMovement = GetComponent<PlayerMovement>();
            _playerState = GetComponent<PlayerState>();
            _playerCharacteristic = GetComponent<PlayerCharacteristic>();
            _worldCharacteristics = FindObjectOfType<WorldCharacteristic>();
            _controller = GetComponent<CharacterController>();
            _jetpack = GetComponent<PlayerJetpack>();
        }
        catch
        {
            Debug.LogError("Ошибка инициализации компонентов");
        }
    }

    private void Start()
    {
        currentJumpCount = defaultJumpCount;
        _lastCheckpoint = transform.position;
        _wasGrounded = _playerState.isGrounded;

        _coyoteTime = Environment.IsDesktop ? 0.1f : 0.3f;
    }

    private void Update()
    {
        if (_ladderDetachTimer > 0f)
            _ladderDetachTimer -= Time.deltaTime;

        if (_playerState.canMove)
        {
            _playerMovement.direction = new Vector3(_inputsManager.inputDirection.x, 0f, _inputsManager.inputDirection.y);

            if (_player.animator != null)
                _player.animator.SetFloat("Magnitude", _playerMovement.direction.magnitude);
        }
        else
        {
            if (_player.animator != null)
                _player.animator.SetFloat("Magnitude", 0f);
        }

        Gravity();
        if (_jetpack != null && _jetpack.isActiveAndEnabled)
        {
            _jetpack.ApplyFlight(Time.deltaTime);
            _isFalling = false;
            _fallStartHeight = transform.position.y;
        }
        else CheckFalling();
        UpdateCoyoteTime();

        if (_playerState.isDeath)
        {
            NullPlayerState();
            return;
        }

        if (!_playerState.isPushed)
        {
            // ВАЖНО: сначала проверяем "спрыг с лестницы", чтобы Jump() не сбросил флаг jump
            if (enableJumpOffLadderBack && _playerState.onLadder)
            {
                if (TryJumpOffLadderBack())
                {
                    // уже отлипли и дали импульс — дальше в этом кадре не лазим/не прыгаем обычным образом
                    _controller.Move(velocity * Time.deltaTime); // [web:229]
                    return;
                }
            }

            Jump();
            CheckLadder();
            ClimbLadder();
            Movement();
        }

        _controller.Move(velocity * Time.deltaTime); // [web:229]
    }

    private void LateUpdate()
    {
        RotateCameraHandler();
        CameraRotation();
    }

    private void CheckFalling()
    {
        if (!_playerState.isGrounded && velocity.y < 0 && !_isFalling && !_playerState.onLadder)
        {
            _isFalling = true;
            _fallStartHeight = transform.position.y;
        }
        else if (_playerState.isGrounded && _isFalling)
        {
            _isFalling = false;
            float fallDistance = _fallStartHeight - transform.position.y;

            if (fallDistance >= _fallDeathHeight)
            {
                OnFallDeath?.Invoke();
            }
        }
        else if (_playerState.onLadder && _isFalling)
        {
            _isFalling = false;
        }
    }

    private void Movement()
    {
        if (_playerState.onLadder)
            return;

        float speed = 0f;
        float targetRotation = 0f;

        if (_playerState.canMove)
        {
            if (_inputsManager.inputDirection != Vector2.zero)
            {
                if (!OnlyRun)
                {
                    speed = _inputsManager.run ? _playerMovement.currentMoveSpeed * 6f : _playerMovement.currentMoveSpeed * 3f;
                    _playerState.isWalking = !_inputsManager.run;
                    _playerState.isRunning = _inputsManager.run;
                }
                else
                {
                    _playerState.isRunning = true;
                    speed = _playerMovement.currentMoveSpeed * 6;
                }

                targetRotation = Quaternion.LookRotation(_playerMovement.direction).eulerAngles.y + _camera.transform.rotation.eulerAngles.y;
                Quaternion rotation = Quaternion.Euler(0f, targetRotation, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 20 * Time.deltaTime);
            }
            else
            {
                _playerState.isWalking = false;
                _playerState.isRunning = false;
            }
        }
        else
        {
            _playerState.isWalking = false;
            _playerState.isRunning = false;
        }

        Vector3 targetDirection = Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;

        if (!OnlyRun)
        {
            if (_player.animator != null)
            {
                _player.animator.SetBool("isWalking", _playerState.isWalking);
                _player.animator.SetBool("isRunning", _playerState.isRunning);
            }
        }
        else
        {
            if (_player.animator != null)
                _player.animator.SetBool("isRunning", _inputsManager.inputDirection != Vector2.zero);
        }

        _controller.Move(targetDirection * speed * Time.deltaTime);
    }

    private void CheckLadder()
    {
        if (_ladderDetachTimer > 0f)
        {
            _playerState.onLadder = false;
            _playerState.isClimbing = false;
            _ladderTarget = null;
            _useLadder = false;
            _player.animator.SetBool("isLadder", false);
            return;
        }

        Collider[] colliders = Physics.OverlapSphere(_ladderEye.position, 0.75f, _ladderLayer);

        foreach (Collider collider in colliders)
        {
            if (collider)
            {
                _playerState.onLadder = true;
                if (_ladderTarget == null || collider.transform != _ladderTarget)
                {
                    if (velocity.x != 0) velocity.x = 0;
                    if (velocity.z != 0) velocity.z = 0;
                    _ladderTarget = collider.transform;
                }
                break;
            }
        }

        if (colliders.Length == 0)
        {
            if (_ladderTarget != null) _ladderTarget = null;
            _playerState.onLadder = false;
            _playerState.isClimbing = false;
        }

        if (_playerState.onLadder)
        {
            if (!_useLadder) _player.animator.SetTrigger("goLadder");

            if (_ladderTarget != null)
            {
                Vector3 targetDirection = _ladderTarget.position - transform.position;
                targetDirection.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 20 * Time.deltaTime);
            }

            _useLadder = true;
        }
        else
        {
            _useLadder = false;
        }

        if (_playerState.isGrounded && _playerState.onLadder)
        {
            if (_inputsManager.inputDirection.y <= -0.1f)
                _playerState.onLadder = false;
        }

        if (_player.animator != null)
            _player.animator.SetBool("isLadder", _playerState.onLadder);
    }

    private void ClimbLadder()
    {
        if (!_playerState.onLadder) return;

        float climbSpeed = 4f;
        float verticalInput = _inputsManager.inputDirection.y;
        Vector3 ladderDirection = Vector3.up * verticalInput * climbSpeed;

        _playerState.isClimbing = ladderDirection != Vector3.zero;

        _controller.Move(ladderDirection * Time.deltaTime);
    }

    private bool TryJumpOffLadderBack()
    {
        // Спрыг делаем по Jump, иначе ты никогда не отлипнешь, если не жмёшь "назад".
        if (!_inputsManager.jump) return false;

        if (requireBackAndJump)
        {
            bool pressingBack = _inputsManager.inputDirection.y <= ladderBackThreshold;
            if (!pressingBack) return false;
        }

        // Отлепляемся
        _playerState.onLadder = false;
        _playerState.isClimbing = false;
        _ladderTarget = null;
        _useLadder = false;
        _player.animator.SetBool("isLadder", false);

        _ladderDetachTimer = ladderDetachTime;

        Vector3 impulse = (-transform.forward * ladderJumpBackForce) + (Vector3.up * ladderJumpUpForce);
        velocity.x = impulse.x;
        velocity.z = impulse.z;
        velocity.y = impulse.y;

        _inputsManager.jump = false;

        _player.animator.SetTrigger("JumpInAir");
        return true;
    }

    private void CameraRotation()
    {
        _xRotation = Mathf.Clamp(_xRotation, -30f, 70f);
        Quaternion rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
        _playerCenter.rotation = rotation;
    }

    private void RotateCameraHandler()
    {
        if (_inputsManager.screenClamped)
        {
            if (!Environment.IsDesktop)
            {
                _xRotation -= SimpleInput.GetAxis("LookY") * _playerCharacteristic.CAMERA_SENSITIVITY * Time.deltaTime;
                _yRotation += SimpleInput.GetAxis("LookX") * _playerCharacteristic.CAMERA_SENSITIVITY * Time.deltaTime;
            }
            else
            {
                _xRotation -= Input.GetAxis("Mouse Y") * _playerCharacteristic.CAMERA_SENSITIVITY * Time.deltaTime;
                _yRotation += Input.GetAxis("Mouse X") * _playerCharacteristic.CAMERA_SENSITIVITY * Time.deltaTime;
            }
        }
    }

    [Header("Проверка земли")]
    [SerializeField] private float _groundCheckDistance = 0.12f;
    [SerializeField] private float _groundCheckRadius = 0.25f;
    [SerializeField] private bool _drawGroundGizmo = true;

    private bool _customGrounded;
    private Vector3 _groundCheckPosition;

    private bool IsGrounded()
    {
        if (_controller == null)
            return false;

        Bounds bounds = _controller.bounds;

        // Точка чуть ниже нижней части CharacterController.
        _groundCheckPosition = new Vector3(
            bounds.center.x,
            bounds.min.y + _groundCheckDistance,
            bounds.center.z
        );

        bool grounded = Physics.CheckSphere(
            _groundCheckPosition,
            _groundCheckRadius,
            _groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (grounded)
        {
            currentJumpCount = defaultJumpCount;
        }

        return grounded;
    }

    public void SetJumpCount(int count)
    {
        defaultJumpCount = count;
    }

    private void UpdateCoyoteTime()
    {
        bool isCurrentlyGrounded = _playerState.isGrounded;

        if (isCurrentlyGrounded)
        {
            _coyoteTimeCounter = _coyoteTime;
            _wasGrounded = true;
        }
        else if (_wasGrounded)
        {
            _coyoteTimeCounter -= Time.deltaTime;
            if (_coyoteTimeCounter <= 0) _wasGrounded = false;
        }
    }

    private bool CanJump()
    {
        return _playerState.isGrounded ||
               (_wasGrounded && _coyoteTimeCounter > 0) ||
               currentJumpCount > 1;
    }

    private void Jump()
    {
        if (_jetpack != null && _jetpack.isActiveAndEnabled)
        {
            _inputsManager.jump = false;
            return;
        }
        if (_inputsManager.jump)
        {
            if (CanJump())
            {
                if (defaultJumpCount > 1)
                {
                    if (currentJumpCount > 1)
                    {
                        SetJump(_playerCharacteristic.PLAYER_JUMP_STRENGTH, true);
                    }
                    else if (currentJumpCount == 1)
                    {
                        DefaultJumpVisual(true);
                        SetJump(_playerCharacteristic.PLAYER_JUMP_STRENGTH, true);
                    }
                }
                else if (currentJumpCount >= 1 && (_playerState.isGrounded || (_wasGrounded && _coyoteTimeCounter > 0)))
                {
                    SetJump(_playerCharacteristic.PLAYER_JUMP_STRENGTH, true);
                    DefaultJumpVisual(false);

                    _coyoteTimeCounter = 0;
                    _wasGrounded = false;
                }
            }

            _inputsManager.jump = false;
        }
    }

    private void DefaultJumpVisual(bool playJumpParticle)
    {
        _audioSource.PlayOneShot(_jumpUpClip);
        if (_jumpParticle != null && playJumpParticle)
            _jumpParticle.Play();
    }

    public void SetJump(float force, bool minusJump)
    {
        if (defaultJumpCount > 1)
        {
            if (_playerState.isWalking && currentJumpCount > 1 || _playerState.isRunning && currentJumpCount > 1)
                _player.animator.SetTrigger("JumpInWalking");
            else
                _player.animator.SetTrigger("JumpInAir");
        }
        else
        {
            _player.animator.SetTrigger("JumpInWalking");
        }

        if (minusJump)
            currentJumpCount--;

        if (!_playerState.onLadder)
        {
            velocity.y = Mathf.Sqrt(force * _worldCharacteristics.WORLD_GRAVITY);
            return;
        }
        else
        {
            Vector3 jumpDirection = -transform.forward + transform.up;
            jumpDirection.Normalize();
            float jumpForce = Mathf.Sqrt(force * _worldCharacteristics.WORLD_GRAVITY);
            velocity = jumpDirection * jumpForce;
            return;
        }
    }

    private void Gravity()
    {
        if (_player.animator != null)
        {
            _player.animator.SetBool("isGrounded", _playerState.isGrounded);
            _player.animator.SetFloat("Velocity", velocity.y);
        }

        _playerState.isGrounded = IsGrounded();

        if (_playerState.onLadder)
        {
            velocity.y = 0;
            return;
        }

        CorrectionVelocity();

        if (_playerState.isGrounded == false)
        {
            if (velocity.y > _maxGravity)
                velocity.y -= _worldCharacteristics.WORLD_GRAVITY * Time.deltaTime;
        }
        else if (velocity.y < 0)
        {
            velocity.y = 0f;
        }
    }

    private void CorrectionVelocity()
    {
        if (velocity.x > 0)
        {
            velocity.x -= _worldCharacteristics.WORLD_GRAVITY / 2 * Time.deltaTime;
            velocity.x = Mathf.Max(0, velocity.x);
        }
        else if (velocity.x < 0)
        {
            velocity.x += _worldCharacteristics.WORLD_GRAVITY / 2 * Time.deltaTime;
            velocity.x = Mathf.Min(0, velocity.x);
        }

        if (velocity.z > 0)
        {
            velocity.z -= _worldCharacteristics.WORLD_GRAVITY / 2 * Time.deltaTime;
            velocity.z = Mathf.Max(0, velocity.z);
        }
        else if (velocity.z < 0)
        {
            velocity.z += _worldCharacteristics.WORLD_GRAVITY / 2 * Time.deltaTime;
            velocity.z = Mathf.Min(0, velocity.z);
        }
    }

    public void PushPlayer(Vector3 pushDirection, float force, Vector3 pushPoint)
    {
        if (_playerState.isPushed == false)
        {
            NullPlayerState();
            StartCoroutine(Push(pushDirection, force, pushPoint));
        }
    }

    // Called after a level-selection teleport, so old fall/ladder state cannot leak into a new run.
    public void ResetForBuildLevel()
    {
        velocity = Vector3.zero;
        _isFalling = false;
        _fallStartHeight = transform.position.y;
        _lastCheckpoint = transform.position;
        _ladderTarget = null;
        _useLadder = false;
        _ladderDetachTimer = 0;
        _coyoteTimeCounter = 0;
        currentJumpCount = defaultJumpCount;
        if (_playerState != null)
        {
            _playerState.onLadder = false;
            _playerState.isClimbing = false;
            _playerState.isRunning = false;
            _playerState.isWalking = false;
        }
        if (_inputsManager != null)
        {
            _inputsManager.inputDirection = Vector2.zero;
            _inputsManager.jump = false;
            _inputsManager.ScreenClamped(false);
        }
        if (_player != null && _player.animator != null)
        {
            _player.animator.SetBool("isLadder", false);
            _player.animator.SetFloat("Magnitude", 0);
        }
    }

    public void NullPlayerState()
    {
        _playerState.isRunning = false;
        _playerState.onLadder = false;
        _playerState.isClimbing = false;
    }

    private IEnumerator Push(Vector3 pushDirection, float force, Vector3 pushPoint)
    {
        _playerState.isPushed = true;
        _player.animator.SetBool("isPushed", _playerState.isPushed);
        _player.animator.SetTrigger("Push");

        velocity.y = 0f;
        velocity.y += force / 2;
        velocity.x = pushDirection.x * force;
        velocity.z = pushDirection.z * force;

        bool xZero = false;
        bool zZero = false;

        while (!xZero || !zZero)
        {
            if (Mathf.Approximately(velocity.x, 0f)) xZero = true;
            if (Mathf.Approximately(velocity.z, 0f)) zZero = true;

            Vector3 targetDirection = pushPoint - transform.position;
            float angle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, angle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 20 * Time.deltaTime);

            yield return null;
        }

        _playerState.isPushed = false;
        _player.animator.SetBool("isPushed", _playerState.isPushed);
    }
}

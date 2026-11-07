using UnityEngine;

public class InputsManager : MonoBehaviour
{
    [Header("Touch controls"), Tooltip("Use the existing joystick and touch camera, including Editor preview.")]
    public bool forceTouchControls;
    private bool UseDesktopControls { get { return !forceTouchControls && Environment.IsDesktop; } }
    public Vector2 inputDirection;

    public bool thisRealPlayer;

    public bool run;
    public bool jump;

    public bool screenClamped; // Ёкран зажат

    [SerializeField] private GameObject _desktopController;
    [SerializeField] private GameObject _mobileController;

    [SerializeField] private GameObject[] _desktopObjectActive;
    [SerializeField] private GameObject[] _mobileObjectActive;

    private void Start()
    {
        if (!thisRealPlayer)
            return;

        if (UseDesktopControls)
            SelectType(true);
        else
            SelectType(false);

        for (int i = 0; i < _desktopObjectActive.Length; i++)
        {
            _desktopObjectActive[i].SetActive(UseDesktopControls);
        }

        for (int j = 0; j < _mobileObjectActive.Length; j++)
        {
            _mobileObjectActive[j].SetActive(!UseDesktopControls);
        }
    }

    private void Update()
    {
        InputDirectionHandler();
        JumpHandler();
        RunHandler();

        if (UseDesktopControls)
        {
            if (Input.GetMouseButtonDown(1))
            {
                ScreenClamped(true);
            }
            if (Input.GetMouseButtonUp(1))
            {
                ScreenClamped(false);
            }
        }
    }

    public void SelectType(bool isDesktop)
    {
        _desktopController.SetActive(isDesktop);
        _mobileController.SetActive(!isDesktop);
    }

    private void InputDirectionHandler()
    {
        if (UseDesktopControls)
            inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        else
            inputDirection = new Vector3(SimpleInput.GetAxis("Horizontal"), SimpleInput.GetAxis("Vertical"));
    }

    private void JumpHandler()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump(true);
        }
    }

    public void Jump(bool value)
    {
        jump = true;
    }

    private void RunHandler()
    {
        Run(Input.GetKey(KeyCode.LeftShift));
    }

    public void Run(bool value)
    {
        run = value;
    }

    public void ScreenClamped(bool value)
    {
        screenClamped = value;

        if (UseDesktopControls)
        {
            if (screenClamped)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;
            }
        }
    }
}

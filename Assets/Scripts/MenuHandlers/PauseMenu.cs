using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    public bool IsPaused { get; private set; }


    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject movelistUI;

    // Cache so unpausing restores whatever timescale was active
    // (e.g. if paused mid-hitstop, we don't want to snap to 1 incorrectly —
    // hitstop manager handles that edge case, see HitStopManager changes below)
    private float _preHitstopTimescale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        PlayerInputHub.Instance.UICancel.performed += OnPauseToggle;
        Debug.Log(PlayerInputHub.Instance.UICancel.enabled);  
    }

    private void OnDisable()
    {   
          if (PlayerInputHub.Instance != null)
            PlayerInputHub.Instance.UICancel.performed -= OnPauseToggle;
    }

    private void OnPauseToggle(InputAction.CallbackContext ctx)
    {
        Debug.Log("Cancel fired");
         //Debug.Log("Cancel fired, IsPaused=" + IsPaused);
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
         if (IsPaused) return;
        IsPaused = true;

        _preHitstopTimescale = Time.timeScale;
        Time.timeScale = 0f;

        PlayerInputHub.Instance.SwitchToUI();
        pauseMenuUI.SetActive(true);

        Debug.Log("[Pause] Paused");
    }

    public void Resume()
    {
         if (!IsPaused) return;
        IsPaused = false;

        if (!HitStopManager.Instance.IsHitstopActive)
            Time.timeScale = 1f;
        // else: HitStopManager's coroutine will restore it when its
        // real-time wait finishes (see HitStopManager change below)

        PlayerInputHub.Instance.SwitchToGameplay();
        pauseMenuUI.SetActive(false);

        Debug.Log("[Pause] Resumed");
    }

    public void Restart()
    {
        Resume();
        UnityEngine.SceneManagement.SceneManager.LoadScene("TAScene");
    }
    public void OpenMovelist()
    {
        movelistUI.SetActive(true);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
        private void Update()
    {
                Debug.Log(PlayerInputHub.Instance.UICancel.enabled);

    }
}



using UnityEngine;
using TMPro;
using System;

public enum CameraAngle
{
    menu = 0,
    whiteTeam = 1,
    blackTeam = 2,
}

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { set; get; }
    [SerializeField] private Animator menuAnimator;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private GameObject[] cameraAngles;
    public Server server;
    public Client client;

    public Action<bool> SetLocalGame;

    private void Awake()
    {
        Instance = this;
        RegisterEvents();
    }
    // cameras
    public void ChangeCamera(CameraAngle index)
    {
        for(int i = 0; i < cameraAngles.Length; i++)
            cameraAngles[i].SetActive(false);

        cameraAngles[(int)index].SetActive(true);
    }
    // buttons
    public void OnNewGameButton()
    {
        menuAnimator.SetTrigger("ChooseTeam");
    }
    public void OnLocalGameButton()
    {
        menuAnimator.SetTrigger("InGameMenu");
        SetLocalGame?.Invoke(true);
        server.Init(9000);
        client.Init("127.0.0.1", 9000);
    }
    public void OnOnelineGameButton()
    {
        menuAnimator.SetTrigger("OnlineMenu");
    }
    public void OnOnelineHostButton()
    {
        server.Init(8000);
        client.Init("127.0.0.1", 8000);
        menuAnimator.SetTrigger("HostMenu");
        SetLocalGame?.Invoke(false);
    }
    public void OnOnelineConnectButton()
    {
        SetLocalGame?.Invoke(false);
        client.Init(addressInput.text, 8000);
    }
    public void OnOnelineBackButton()
    {
        menuAnimator.SetTrigger("StartMenu");
    }
    public void OnHostBackButton()
    {
        server.Shutdown();
        client.Shutdown();
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnLeaveFromGameMenu()
    {
        ChangeCamera(CameraAngle.menu);
        menuAnimator.SetTrigger("StartMenu");
    }

    #region
    private void RegisterEvents()
    {
        NetUtility.C_START_GAME += OnStartGameClient;
    }
    private void UnregisterEvents()
    {
        NetUtility.C_START_GAME -= OnStartGameClient;
    }
    private void OnStartGameClient(NetMessage msg)
    {
        menuAnimator.SetTrigger("InGameMenu");
    }
    #endregion
}

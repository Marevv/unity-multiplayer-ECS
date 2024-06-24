using System;
using TMPro;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; set; }

    [SerializeField] private Transform logInUiPanel;
    [SerializeField] private Transform mainMenuUiPanel;
    [SerializeField] private Transform inGamePanel;
    [SerializeField] private TMP_Text nameLabelText;
    [SerializeField] private TMP_Text lobbyJoinCodeText;
    [SerializeField] private TMP_Text addressText;
    [SerializeField] private TMP_Text portText;
    [SerializeField] private TMP_InputField nameToChangeInputField;
    [SerializeField] private TMP_InputField lobbyCodeInputField;
    [SerializeField] private TMP_InputField addressInputField;
    [SerializeField] private TMP_InputField portInputField;
    [SerializeField] private Button joinButton;
    private string _oldValue;
    

    //private string _playerName;

    public string PlayerName
    {
        get => nameToChangeInputField.text;
        set
        {
            nameLabelText.text = value;
            nameToChangeInputField.text = "";
        }
    }

    public string LobbyJoinCode
    {
        get => lobbyCodeInputField.text;
        set => lobbyJoinCodeText.text = value;
    }

    public string Address
    {
        get => addressInputField.text;
    }

    public string Port
    {
        get => portInputField.text;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LogInUI();
        
        joinButton.onClick.AddListener(ConnectionManager.Instance.Connect);
    }


    public void LogInUI()
    {
        logInUiPanel.gameObject.SetActive(true);
        mainMenuUiPanel.gameObject.SetActive(false);
        inGamePanel.gameObject.SetActive(false);
    }

    public void MainMenuUI()
    {
        logInUiPanel.gameObject.SetActive(false);
        mainMenuUiPanel.gameObject.SetActive(true);
        inGamePanel.gameObject.SetActive(false);
    }

    public void InGameUI()
    {
        logInUiPanel.gameObject.SetActive(false);
        mainMenuUiPanel.gameObject.SetActive(false);
        inGamePanel.gameObject.SetActive(true);
    }
    
    public void OnRelayEnable(Toggle value)
    {
        if (value.isOn)
        {
            addressText.text = "Join Code:";
            addressInputField.placeholder.GetComponent<TMP_Text>().text = "Enter Relay Code";
            portText.gameObject.SetActive(false);
            portInputField.gameObject.SetActive(false);
            _oldValue = addressInputField.text;
            addressInputField.text = string.Empty;
            ConnectionManager.Instance.UseRelay = true;
            joinButton.onClick.AddListener(ConnectionManager.Instance.JoinRelayWithCode);
            joinButton.onClick.RemoveListener(ConnectionManager.Instance.Connect);
        }
        else
        {
            addressText.text = "IP:";
            portText.gameObject.SetActive(true);
            portInputField.gameObject.SetActive(true);
            addressInputField.text = _oldValue;
            addressInputField.placeholder.GetComponent<TMP_Text>().text = "Enter IP Address";
            ConnectionManager.Instance.UseRelay = false;
            joinButton.onClick.RemoveListener(ConnectionManager.Instance.JoinRelayWithCode);
            joinButton.onClick.AddListener(ConnectionManager.Instance.Connect);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
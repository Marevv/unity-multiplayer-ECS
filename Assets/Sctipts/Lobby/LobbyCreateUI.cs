using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;

public class LobbyCreateUI : MonoBehaviour {


    public static LobbyCreateUI Instance { get; private set; }


    [SerializeField] private Button createButton;
    [SerializeField] private TMP_InputField lobbyNameInputField;
    [SerializeField] private Toggle publicPrivateToggle;
    [SerializeField] private TMP_InputField maxPlayersInputfield;
    [SerializeField] private TMP_Dropdown gameModeDropdown;


    private string lobbyName;
    private bool isPrivate;
    private int maxPlayers;
    private LobbyManager.GameMode gameMode;

    private void Awake() {
        Instance = this;

        createButton.onClick.AddListener(() => {
            LobbyManager.Instance.CreateLobby(
                lobbyNameInputField.text,
                int.Parse(maxPlayersInputfield.text),
                publicPrivateToggle.isOn,
                (LobbyManager.GameMode)gameModeDropdown.value
            );
        });
    }

}
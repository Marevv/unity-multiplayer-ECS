using System;
using System.Text;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class PlayerAccounts : MonoBehaviour
{
    [SerializeField] private GameObject beforeLogInPanel;
    [SerializeField] private TMP_Text nameLabelText;
    [SerializeField] private TMP_InputField nameToChangeInputField;
    [SerializeField] private GameObject afterLogInPanel;

    private string _externalIds;

    async void Awake()
    {
        await UnityServices.InitializeAsync();
        PlayerAccountService.Instance.SignedIn += SignInWithUnity;
    }
    
    

    public async void StartSignInAsync(bool anonymously)
    {
        if (PlayerAccountService.Instance.IsSignedIn)
        {
            SignInWithUnity();
            AfterSignIn();
            return;
        }

        try
        {
            if(anonymously)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            else
                await PlayerAccountService.Instance.StartSignInAsync();
            
            AfterSignIn();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }
    }

    private async void AfterSignIn()
    {
        beforeLogInPanel.SetActive(false);
        afterLogInPanel.SetActive(true);
        try
        {
            await AuthenticationService.Instance.GetPlayerNameAsync();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }


        nameLabelText.text = AuthenticationService.Instance.PlayerName;
    }

    public async void ChangePlayerName()
    {
        await AuthenticationService.Instance.UpdatePlayerNameAsync(nameToChangeInputField.text);
        nameLabelText.text = AuthenticationService.Instance.PlayerName;
        nameToChangeInputField.text = "";
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        beforeLogInPanel.SetActive(true);
        afterLogInPanel.SetActive(false);
    }

    async void SignInWithUnity()
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }
    }
}
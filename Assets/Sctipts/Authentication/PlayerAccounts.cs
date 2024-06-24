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

        UiManager.Instance.MainMenuUI();

        UiManager.Instance.PlayerName = AuthenticationService.Instance.PlayerName;
    }

    public async void ChangePlayerName()
    {
        await AuthenticationService.Instance.UpdatePlayerNameAsync(UiManager.Instance.PlayerName);
        UiManager.Instance.PlayerName = AuthenticationService.Instance.PlayerName;
    }

    //This is just to be able to test on same PC
    public async void Authenticate(TMP_InputField playerName)
    {
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName.text);

        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed in with ID: {AuthenticationService.Instance.PlayerId}");
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        AfterSignIn();
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        UiManager.Instance.LogInUI();
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
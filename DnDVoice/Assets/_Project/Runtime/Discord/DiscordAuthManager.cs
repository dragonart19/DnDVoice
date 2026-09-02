using System;
using Discord.Sdk;
using UnityEngine;

namespace DndProximityVoice.Discord
{
    [DisallowMultipleComponent]
    public sealed class DiscordAuthManager : MonoBehaviour
    {
        private Client client;
        private string codeVerifier = string.Empty;

        public event Action<DiscordAuthState> StateChanged;

        public DiscordAuthState State { get; private set; } = DiscordAuthState.Initializing;

        public string ErrorMessage { get; private set; } = string.Empty;

        public DiscordUserSnapshot CurrentUser { get; private set; }

        public Client Client => client;

        public bool CanBeginLogin => State == DiscordAuthState.ReadyToLogin ||
                                     State == DiscordAuthState.Failed;

        private void Awake()
        {
            try
            {
                NativeMethods.UnhandledException += OnSdkUnhandledException;
                client = new Client();
                client.SetApplicationId(DiscordConfiguration.ApplicationId);
                client.AddLogCallback(OnSdkLog, LoggingSeverity.Warning);
                client.SetStatusChangedCallback(OnClientStatusChanged);
                SetState(DiscordAuthState.ReadyToLogin);
            }
            catch (Exception exception)
            {
                Fail("Impossibile inizializzare Discord Social SDK.", exception);
            }
        }

        public void BeginLogin()
        {
            if (!CanBeginLogin || client == null)
            {
                return;
            }

            ErrorMessage = string.Empty;
            CurrentUser = null;

            try
            {
                using (var verifier = client.CreateAuthorizationCodeVerifier())
                using (var challenge = verifier.Challenge())
                using (var args = new AuthorizationArgs())
                {
                    codeVerifier = verifier.Verifier();
                    args.SetClientId(DiscordConfiguration.ApplicationId);
                    args.SetScopes(DiscordConfiguration.RequiredScopes);
                    args.SetCodeChallenge(challenge);

                    SetState(DiscordAuthState.Authorizing);
                    client.Authorize(args, OnAuthorizeCompleted);
                }
            }
            catch (Exception exception)
            {
                Fail("Non è stato possibile avviare il login Discord.", exception);
            }
        }

        private void OnAuthorizeCompleted(ClientResult result, string code, string redirectUri)
        {
            if (!result.Successful())
            {
                Fail($"Autorizzazione Discord non riuscita: {result.Error()}");
                return;
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(codeVerifier))
            {
                Fail("Discord non ha restituito un codice di autorizzazione valido.");
                return;
            }

            SetState(DiscordAuthState.ExchangingToken);
            client.GetToken(
                DiscordConfiguration.ApplicationId,
                code,
                codeVerifier,
                redirectUri,
                OnTokenReceived);
        }

        private void OnTokenReceived(
            ClientResult result,
            string accessToken,
            string refreshToken,
            AuthorizationTokenType tokenType,
            int expiresIn,
            string scopes)
        {
            codeVerifier = string.Empty;

            if (!result.Successful() || string.IsNullOrWhiteSpace(accessToken))
            {
                Fail($"Scambio del token Discord non riuscito: {result.Error()}");
                return;
            }

            SetState(DiscordAuthState.Connecting);
            client.UpdateToken(AuthorizationTokenType.Bearer, accessToken, OnTokenUpdated);
        }

        private void OnTokenUpdated(ClientResult result)
        {
            if (!result.Successful())
            {
                Fail($"Discord ha rifiutato il token: {result.Error()}");
                return;
            }

            client.Connect();
        }

        private void OnClientStatusChanged(Client.Status status, Client.Error error, int errorDetail)
        {
            if (error != Client.Error.None)
            {
                Fail($"Connessione Discord interrotta ({error}, codice {errorDetail}).");
                return;
            }

            switch (status)
            {
                case Client.Status.Ready:
                    CaptureCurrentUser();
                    SetState(DiscordAuthState.Connected);
                    break;
                case Client.Status.Connecting:
                case Client.Status.Connected:
                case Client.Status.Reconnecting:
                case Client.Status.HttpWait:
                    SetState(DiscordAuthState.Connecting);
                    break;
                case Client.Status.Disconnected:
                    if (State == DiscordAuthState.Connected || State == DiscordAuthState.Connecting)
                    {
                        Fail("Discord si è disconnesso.");
                    }
                    break;
            }
        }

        private void CaptureCurrentUser()
        {
            using (var user = client.GetCurrentUserV2())
            {
                if (user == null)
                {
                    CurrentUser = null;
                    return;
                }

                CurrentUser = new DiscordUserSnapshot(
                    user.Id(),
                    user.Username(),
                    user.DisplayName(),
                    user.AvatarUrl(UserHandle.AvatarType.Gif, UserHandle.AvatarType.Png));
            }
        }

        private static void OnSdkLog(string message, LoggingSeverity severity)
        {
            if (severity >= LoggingSeverity.Error)
            {
                Debug.LogError($"Discord Social SDK: {message}");
            }
            else
            {
                Debug.LogWarning($"Discord Social SDK: {message}");
            }
        }

        private void OnSdkUnhandledException(Exception exception)
        {
            Fail("Discord Social SDK ha generato un errore imprevisto.", exception);
        }

        private void SetState(DiscordAuthState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }

        private void Fail(string message, Exception exception = null)
        {
            codeVerifier = string.Empty;
            ErrorMessage = message;

            if (exception == null)
            {
                Debug.LogError(message);
            }
            else
            {
                Debug.LogException(new InvalidOperationException(message, exception));
            }

            SetState(DiscordAuthState.Failed);
        }

        private void OnDestroy()
        {
            NativeMethods.UnhandledException -= OnSdkUnhandledException;
            client?.Dispose();
            client = null;
        }
    }
}

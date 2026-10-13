using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using UnityEngine;

public struct VoiceParticipantInfo
{
    public string Name;
    public bool Speaking;
    public bool IsSelf;
}

public class VoiceChat : MonoBehaviour
{
    public int audibleDistance = 40;
    public int conversationalDistance = 5;
    public float fadeIntensity = 0.5f;
    public float positionInterval = 0.1f;

    string channel;
    float nextPositionUpdate;

    public event Action Changed;

    public bool IsActive => channel != null;

    public bool IsMuted { get; private set; }

    public string Status { get; private set; } = "";

    public async Task JoinAsync(string channelName, string displayName)
    {
        try
        {
            Status = "Sprachchat wird gestartet...";
            Changed?.Invoke();

            await RequestMicrophoneAsync();
            await VivoxService.Instance.InitializeAsync();

            if (!VivoxService.Instance.IsLoggedIn)
                await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = Sanitize(displayName) });

            await VivoxService.Instance.JoinPositionalChannelAsync(
                channelName,
                ChatCapability.AudioOnly,
                new Channel3DProperties(audibleDistance, conversationalDistance, fadeIntensity, AudioFadeModel.InverseByDistance));

            channel = channelName;
            IsMuted = VivoxService.Instance.IsInputDeviceMuted;
            Status = "Sprachchat aktiv";
        }
        catch (Exception e)
        {
            channel = null;
            Status = "Sprachchat nicht verfügbar: " + e.Message;
            Debug.LogWarning("VoiceChat: " + e);
        }

        Changed?.Invoke();
    }

    public async Task LeaveAsync()
    {
        if (channel == null) return;

        channel = null;
        try
        {
            await VivoxService.Instance.LeaveAllChannelsAsync();
            await VivoxService.Instance.LogoutAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning("VoiceChat: " + e.Message);
        }

        Status = "";
        Changed?.Invoke();
    }

    public void SetMuted(bool muted)
    {
        if (!IsActive) return;

        if (muted) VivoxService.Instance.MuteInputDevice();
        else VivoxService.Instance.UnmuteInputDevice();

        IsMuted = muted;
        Changed?.Invoke();
    }

    public List<VoiceParticipantInfo> GetParticipants()
    {
        var result = new List<VoiceParticipantInfo>();
        if (channel == null || !VivoxService.Instance.ActiveChannels.TryGetValue(channel, out var participants))
            return result;

        foreach (var participant in participants)
        {
            result.Add(new VoiceParticipantInfo
            {
                Name = participant.DisplayName,
                Speaking = participant.SpeechDetected,
                IsSelf = participant.IsSelf
            });
        }
        return result;
    }

    void Update()
    {
        if (channel == null || Time.unscaledTime < nextPositionUpdate) return;
        nextPositionUpdate = Time.unscaledTime + positionInterval;

        var camera = Camera.main;
        if (camera == null || !VivoxService.Instance.ActiveChannels.ContainsKey(channel)) return;

        var head = camera.transform;
        VivoxService.Instance.Set3DPosition(head.position, head.position, head.forward, head.up, channel);
    }

    static async Task RequestMicrophoneAsync()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            for (int i = 0; i < 60 && !UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone); i++)
                await Task.Delay(500);
        }
#else
        await Task.CompletedTask;
#endif
    }

    static string Sanitize(string name)
    {
        var builder = new StringBuilder();
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_') builder.Append(c);
        }

        string clean = builder.ToString().Trim();
        if (clean.Length == 0) clean = "Teilnehmer";
        if (clean.Length > 40) clean = clean.Substring(0, 40);
        return clean + "-" + UnityEngine.Random.Range(100, 1000);
    }
}

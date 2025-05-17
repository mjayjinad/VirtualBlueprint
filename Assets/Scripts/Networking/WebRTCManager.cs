using NativeWebSocket;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

public class WebRTCManager : MonoBehaviour
{
    [Header("Video Transmission")]
    [SerializeField] private Camera cameraStream;
    [SerializeField] private RawImage sourceImage;
    [SerializeField] private RawImage receiveImage;

    [Header("Audio Transmission")]
    [SerializeField] private AudioSource inputAudioSource;
    [SerializeField] private AudioSource outputAudioSource;

    private bool startVideoAudioChannel = false;
    private RTCPeerConnection peerConnection;
    private AudioStreamTrack micAudioTrack;
    private WebSocket webSocket;
    private string clientId;

    private bool hasReceivedOffer = false;
    private SessionDescription receivedOfferSessionDescTemp;

    private bool hasReceivedAnswer = false;
    private SessionDescription receivedAnswerSessionDescTemp;
    private bool videoStreamStarted = false;

    private async void Start()
    {
        InitializeMicStatus();

        clientId = gameObject.name;

        // Initialize WebSocket
        //The websocket signalling server(wss://webrtc-bim-server.glitch.me) will be closed.
        //If you need to test the application for recruitement purporses, contact me at abdulmaliq.jinad@gmail.com
        webSocket = new WebSocket("wss://webrtc-bim-server.glitch.me/", new Dictionary<string, string>() {
            { "user-agent", "unity webrtc" }
        });

        webSocket.OnOpen += () => {
            // STUN server config
            RTCConfiguration config = default;
            config.iceServers = new[] {
                new RTCIceServer {
                    urls = new[] {
                        "stun:stun.l.google.com:19302"
                    }
                }
            };

            peerConnection = new RTCPeerConnection(ref config);
            peerConnection.OnIceCandidate = candidate => {
                var candidateInit = new CandidateInit()
                {
                    SdpMid = candidate.SdpMid,
                    SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                    Candidate = candidate.Candidate
                };
                webSocket.SendText("CANDIDATE!" + candidateInit.ConvertToJSON());
            };
            peerConnection.OnIceConnectionChange = state => {
                Debug.Log(state);

                if (state == RTCIceConnectionState.Disconnected ||
                    state == RTCIceConnectionState.Failed ||
                    state == RTCIceConnectionState.Closed)
                {
                    OnPeerDisconnected();
                }
            };

            peerConnection.OnTrack = e => {
                if (e.Track is VideoStreamTrack video)
                {
                    if(receiveImage != null)
                    {
                        video.OnVideoReceived += tex => {
                            receiveImage.texture = tex;

                            if (!videoStreamStarted)
                            {
                                videoStreamStarted = true;
                            }
                        };
                    }
                }
                if (e.Track is AudioStreamTrack audio)
                {
                    outputAudioSource.SetTrack(audio);
                    outputAudioSource.loop = true;
                    outputAudioSource.Play();
                }
            };

            peerConnection.OnNegotiationNeeded = () => {
                StartCoroutine(CreateOffer());
            };

            StartCoroutine(WebRTC.Update());
        };

        // Receive WebSocket messages
        webSocket.OnMessage += (bytes) => {
            var data = Encoding.UTF8.GetString(bytes);
            var signalingMessage = new SignalingMessage(data);

            switch (signalingMessage.Type)
            {
                case SignalingMessageType.OFFER:
                    Debug.Log(clientId + " - Got OFFER: " + signalingMessage.Message);
                    receivedOfferSessionDescTemp = SessionDescription.FromJSON(signalingMessage.Message);
                    hasReceivedOffer = true;
                    break;
                case SignalingMessageType.ANSWER:
                    Debug.Log(clientId + " - Got ANSWER: " + signalingMessage.Message);
                    receivedAnswerSessionDescTemp = SessionDescription.FromJSON(signalingMessage.Message);
                    hasReceivedAnswer = true;
                    break;
                case SignalingMessageType.CANDIDATE:
                    Debug.Log(clientId + " - Got CANDIDATE: " + signalingMessage.Message);
                    var candidateInit = CandidateInit.FromJSON(signalingMessage.Message);
                    RTCIceCandidateInit init = new RTCIceCandidateInit();
                    init.sdpMid = candidateInit.SdpMid;
                    init.sdpMLineIndex = candidateInit.SdpMLineIndex;
                    init.candidate = candidateInit.Candidate;
                    RTCIceCandidate candidate = new RTCIceCandidate(init);
                    peerConnection.AddIceCandidate(candidate);
                    break;
                default:
                    Debug.Log(clientId + " - Received: " + data);
                    break;
            }
        };

        await webSocket.Connect();
    }

    private void Update()
    {
        if (hasReceivedOffer)
        {
            hasReceivedOffer = !hasReceivedOffer;
            StartCoroutine(CreateAnswer());
        }
        if (hasReceivedAnswer)
        {
            hasReceivedAnswer = !hasReceivedAnswer;
            StartCoroutine(SetRemoteDesc());
        }

        if (startVideoAudioChannel)
        {
            startVideoAudioChannel = !startVideoAudioChannel;

            // Video
            if(sourceImage != null)
            {
                var videoStreamTrack = cameraStream.CaptureStreamTrack(1280, 720);
                sourceImage.texture = cameraStream.targetTexture;
                peerConnection.AddTrack(videoStreamTrack);
            }

            StartMicrophone();
            micAudioTrack = new AudioStreamTrack(inputAudioSource);
            micAudioTrack.Loopback = true;
            peerConnection.AddTrack(micAudioTrack);
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        webSocket.DispatchMessageQueue();
#endif
    }

    private void OnDestroy()
    {
        if (peerConnection != null)
        {
            peerConnection.Close();
        }
        if (webSocket != null)
        {
            webSocket.Close();
        }
    }

    private IEnumerator CreateOffer()
    {
        var offer = peerConnection.CreateOffer();
        yield return offer;

        var offerDesc = offer.Desc;
        var localDescOp = peerConnection.SetLocalDescription(ref offerDesc);
        yield return localDescOp;

        var offerSessionDesc = new SessionDescription()
        {
            SessionType = offerDesc.type.ToString(),
            Sdp = offerDesc.sdp
        };
        webSocket.SendText("OFFER!" + offerSessionDesc.ConvertToJSON());
    }

    private IEnumerator CreateAnswer()
    {
        RTCSessionDescription offerSessionDesc = new RTCSessionDescription();
        offerSessionDesc.type = RTCSdpType.Offer;
        offerSessionDesc.sdp = receivedOfferSessionDescTemp.Sdp;

        var remoteDescOp = peerConnection.SetRemoteDescription(ref offerSessionDesc);
        yield return remoteDescOp;

        var answer = peerConnection.CreateAnswer();
        yield return answer;

        var answerDesc = answer.Desc;
        var localDescOp = peerConnection.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        var answerSessionDesc = new SessionDescription()
        {
            SessionType = answerDesc.type.ToString(),
            Sdp = answerDesc.sdp
        };
        webSocket.SendText("ANSWER!" + answerSessionDesc.ConvertToJSON());
    }

    private IEnumerator SetRemoteDesc()
    {
        RTCSessionDescription answerSessionDesc = new RTCSessionDescription();
        answerSessionDesc.type = RTCSdpType.Answer;
        answerSessionDesc.sdp = receivedAnswerSessionDescTemp.Sdp;

        var remoteDescOp = peerConnection.SetRemoteDescription(ref answerSessionDesc);
        yield return remoteDescOp;
    }

    private void StartMicrophone()
    {
        string micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (micDevice != null)
        {
            inputAudioSource.clip = Microphone.Start(micDevice, true, 10, 48000);
            inputAudioSource.loop = true;

            // Wait until microphone has started recording
            while (!(Microphone.GetPosition(micDevice) > 0)) { }

            inputAudioSource.Play();
        }
        else
        {
            Debug.LogError("No microphone devices found.");
        }
    }

    public void StartVideoAudio()
    {
        startVideoAudioChannel = true;
    }

    private void InitializeMicStatus()
    {
        if (micAudioTrack != null)
        {
            micAudioTrack.Loopback = false;  // Disable microphone audio loopback
        }

        inputAudioSource.volume = 0;  // Mute audio
        Debug.Log("Microphone muted on start");
    }

    public void MuteMic(bool toggleValue)
    {
        if (toggleValue)  // Toggle is On (Unmuted)
        {
            if (micAudioTrack != null)
            {
                micAudioTrack.Loopback = false;  // Disable microphone audio loopback
            }

            inputAudioSource.volume = 0;  // Mute audio
            Debug.Log("Microphone muted");
        }
        else  // Toggle is Off (Muted)
        {
            if (micAudioTrack != null)
            {
                micAudioTrack.Loopback = true;  // Enable microphone audio loopback
            }

            inputAudioSource.volume = 1;  // Restore audio volume
            Debug.Log("Microphone unmuted");
        }
    }

    public string OnVideoStreamStarted()
    {
        string text;
        if (videoStreamStarted)
        {
            text = "";
        }
        else
        {
            text = "The stream hasn’t started yet. Please try again once the user begins streaming";
            Debug.Log("Not connected");
        }

        return text;
    }

    private void OnPeerDisconnected()
    {
        Debug.Log("Remote peer disconnected or connection lost.");
        videoStreamStarted = false;
        OnVideoStreamStarted();
    }
}

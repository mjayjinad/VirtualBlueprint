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
    [SerializeField] private Camera cameraStream; // Camera to capture video stream from
    [SerializeField] private RawImage sourceImage; // UI element showing local camera feed
    [SerializeField] private RawImage receiveImage; // UI element showing received remote video

    [Header("Audio Transmission")]
    [SerializeField] private AudioSource inputAudioSource; // Audio source for microphone input (local)
    [SerializeField] private AudioSource outputAudioSource; // Audio source for remote audio playback

    private bool startVideoAudioChannel = false; // Flag to start video and audio streaming
    private RTCPeerConnection peerConnection; // WebRTC peer connection instance
    private AudioStreamTrack micAudioTrack; // Track for microphone audio
    private WebSocket webSocket; // WebSocket for signaling
    private string clientId; // Unique client identifier

    private bool hasReceivedOffer = false; // Flag for receiving SDP offer
    private SessionDescription receivedOfferSessionDescTemp; // Temporary storage for SDP offer

    private bool hasReceivedAnswer = false; // Flag for receiving SDP answer
    private SessionDescription receivedAnswerSessionDescTemp; // Temporary storage for SDP answer
    private bool videoStreamStarted = false; // Flag to track if video stream has started

    // Called when the script instance is loaded
    private async void Start()
    {
        InitializeMicStatus();

        clientId = gameObject.name; // Use GameObject name as client ID

        // Initialize WebSocket for signaling
        // NOTE: The default signaling server "wss://webrtc-bim-server.glitch.me" will be closed.
        // For recruitment testing, contact abdulmaliq.jinad@gmail.com for access.
        webSocket = new WebSocket("wss://webrtc-bim-server.glitch.me/", new Dictionary<string, string>() {
            { "user-agent", "unity webrtc" }
        });

        // Called when WebSocket connection opens
        webSocket.OnOpen += () => {
            // Configure STUN server for NAT traversal
            RTCConfiguration config = default;
            config.iceServers = new[] {
                new RTCIceServer {
                    urls = new[] { "stun:stun.l.google.com:19302" }
                }
            };

            // Create new WebRTC peer connection with config
            peerConnection = new RTCPeerConnection(ref config);

            // Send ICE candidates over WebSocket when discovered
            peerConnection.OnIceCandidate = candidate => {
                var candidateInit = new CandidateInit()
                {
                    SdpMid = candidate.SdpMid,
                    SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                    Candidate = candidate.Candidate
                };
                webSocket.SendText("CANDIDATE!" + candidateInit.ConvertToJSON());
            };

            // Monitor ICE connection state changes
            peerConnection.OnIceConnectionChange = state => {
                Debug.Log(state);

                // Handle peer disconnection or failure
                if (state == RTCIceConnectionState.Disconnected ||
                    state == RTCIceConnectionState.Failed ||
                    state == RTCIceConnectionState.Closed)
                {
                    OnPeerDisconnected();
                }
            };

            // Handle incoming media tracks
            peerConnection.OnTrack = e => {
                // Video track received
                if (e.Track is VideoStreamTrack video)
                {
                    if (receiveImage != null)
                    {
                        // Update UI texture on receiving video frames
                        video.OnVideoReceived += tex => {
                            receiveImage.texture = tex;

                            // Set flag when video stream starts
                            if (!videoStreamStarted)
                            {
                                videoStreamStarted = true;
                            }
                        };
                    }
                }

                // Audio track received
                if (e.Track is AudioStreamTrack audio)
                {
                    // Set remote audio source and play
                    outputAudioSource.SetTrack(audio);
                    outputAudioSource.loop = true;
                    outputAudioSource.Play();
                }
            };

            // Trigger offer creation when negotiation needed
            peerConnection.OnNegotiationNeeded = () => {
                StartCoroutine(CreateOffer());
            };

            // Start WebRTC update loop coroutine
            StartCoroutine(WebRTC.Update());
        };

        // Handle incoming WebSocket messages (signaling)
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

        // Connect WebSocket
        await webSocket.Connect();
    }

    private void Update()
    {
        // If SDP offer received, start creating an answer
        if (hasReceivedOffer)
        {
            hasReceivedOffer = !hasReceivedOffer;
            StartCoroutine(CreateAnswer());
        }

        // If SDP answer received, set remote description
        if (hasReceivedAnswer)
        {
            hasReceivedAnswer = !hasReceivedAnswer;
            StartCoroutine(SetRemoteDesc());
        }

        // Trigger starting video and audio streaming when flag is set
        if (startVideoAudioChannel)
        {
            startVideoAudioChannel = !startVideoAudioChannel;

            // Start video streaming if UI element assigned
            if (sourceImage != null)
            {
                // Capture video stream track from camera with 1280x720 resolution
                var videoStreamTrack = cameraStream.CaptureStreamTrack(1280, 720);
                sourceImage.texture = cameraStream.targetTexture;
                peerConnection.AddTrack(videoStreamTrack);
            }

            // Start microphone streaming
            StartMicrophone();
            micAudioTrack = new AudioStreamTrack(inputAudioSource);
            micAudioTrack.Loopback = true; // Enable mic audio loopback
            peerConnection.AddTrack(micAudioTrack);
        }

        // Dispatch WebSocket message queue for platforms except WebGL editor
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

    /// <summary>
    /// Coroutine to create SDP offer and send via signaling server
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Coroutine to create SDP answer and send via signaling server
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Coroutine to set remote session description (answer)
    /// </summary>
    /// <returns></returns>
    private IEnumerator SetRemoteDesc()
    {
        RTCSessionDescription answerSessionDesc = new RTCSessionDescription();
        answerSessionDesc.type = RTCSdpType.Answer;
        answerSessionDesc.sdp = receivedAnswerSessionDescTemp.Sdp;

        var remoteDescOp = peerConnection.SetRemoteDescription(ref answerSessionDesc);
        yield return remoteDescOp;
    }

    /// <summary>
    /// Start capturing microphone audio input
    /// </summary>
    private void StartMicrophone()
    {
        string micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (micDevice != null)
        {
            inputAudioSource.clip = Microphone.Start(micDevice, true, 10, 48000);
            inputAudioSource.loop = true;

            // Wait until microphone starts recording
            while (!(Microphone.GetPosition(micDevice) > 0)) { }

            inputAudioSource.Play();
        }
        else
        {
            Debug.LogError("No microphone devices found.");
        }
    }

    /// <summary>
    /// Public method to trigger starting video and audio streaming
    /// </summary>
    public void StartVideoAudio()
    {
        startVideoAudioChannel = true;
    }

    /// <summary>
    /// Initialize microphone mute state on start
    /// </summary>
    private void InitializeMicStatus()
    {
        if (micAudioTrack != null)
        {
            micAudioTrack.Loopback = false;  // Disable microphone audio loopback initially
        }

        inputAudioSource.volume = 0;  // Mute audio initially
    }

    /// <summary>
    /// Mute or unmute the microphone based on toggle value
    /// </summary>
    /// <param name="toggleValue"></param>
    public void MuteMic(bool toggleValue)
    {
        if (toggleValue)
        {
            if (micAudioTrack != null)
            {
                micAudioTrack.Loopback = false;  // Disable microphone audio loopback to mute
            }

            inputAudioSource.volume = 0;  // Mute local mic monitoring audio
        }
        else 
        {
            if (micAudioTrack != null)
            {
                micAudioTrack.Loopback = true;  // Enable microphone audio loopback to unmute
            }

            inputAudioSource.volume = 1;  // Restore local mic monitoring audio
        }
    }

    /// <summary>
    /// Returns a status message depending on whether video stream started
    /// </summary>
    /// <returns></returns>
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
        }

        return text;
    }

    /// <summary>
    /// Called when remote peer disconnects or connection is lost
    /// </summary>
    private void OnPeerDisconnected()
    {
        Debug.Log("Remote peer disconnected or connection lost.");
        videoStreamStarted = false;
        OnVideoStreamStarted();
    }
}

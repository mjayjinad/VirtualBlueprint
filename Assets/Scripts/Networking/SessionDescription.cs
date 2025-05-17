using System;
using UnityEngine;

[Serializable]
public class SessionDescription : IJsonObject<SessionDescription>
{
    public string SessionType; //Type of the session description (e.g., "offer", "answer").

    public string Sdp; //The SDP (Session Description Protocol) string.

    /// <summary>
    /// Serializes this SessionDescription object to a JSON string.
    /// </summary>
    /// <returns></returns>
    public string ConvertToJSON()
    {
        return JsonUtility.ToJson(this);
    }
    /// <summary>
    /// Deserializes a JSON string into a SessionDescription object.
    /// </summary>
    /// <param name="jsonString"></param>
    /// <returns></returns>
    public static SessionDescription FromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SessionDescription>(jsonString);
    }
}
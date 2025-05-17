using System;
using UnityEngine;

[Serializable]
public class CandidateInit : IJsonObject<CandidateInit>
{
    public string Candidate; //The ICE candidate SDP string.

    public string SdpMid; //The media stream identification for the candidate.

    public int SdpMLineIndex; //The index of the media description line in the SDP.

    /// <summary>
    /// Deserialize JSON string into a CandidateInit object.
    /// </summary>
    /// <param name="jsonString"></param>
    /// <returns></returns>
    public static CandidateInit FromJSON(string jsonString)
    {
        return JsonUtility.FromJson<CandidateInit>(jsonString);
    }

    /// <summary>
    /// Serialize this CandidateInit instance to a JSON string.
    /// </summary>
    /// <returns></returns>
    public string ConvertToJSON()
    {
        return JsonUtility.ToJson(this);
    }
}

using System;

public class SignalingMessage
{
    public readonly SignalingMessageType Type; //The type of signaling message (Offer, Answer, Candidate, or Other).

    public readonly string Message; //The content/message payload of the signaling message.

    /// <summary>
    /// Constructor that parses the raw message string into type and content.
    /// </summary>
    /// <param name="messageString"></param>
    public SignalingMessage(string messageString)
    {
        // Default type to OTHER, meaning unrecognized type
        Type = SignalingMessageType.OTHER;
        Message = messageString;

        // Split string by '!' delimiter into [type, payload]
        var messageArray = messageString.Split('!');

        // If split result is valid and first part parses to SignalingMessageType
        if ((messageArray.Length >= 2) && Enum.TryParse(messageArray[0], out SignalingMessageType resultType))
        {
            // Only assign recognized types to Type and update message to payload
            switch (resultType)
            {
                case SignalingMessageType.OFFER:
                case SignalingMessageType.ANSWER:
                case SignalingMessageType.CANDIDATE:
                    Type = resultType;
                    Message = messageArray[1];
                    break;
            }
        }
    }
}
namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents a message response from the server
    /// </summary>
    public class ServerResponse
    {
        /// <summary>
        /// Used to return a response from server to client
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="responseType">The type of the response</param>
        public ServerResponse(string message, ServerResponseType responseType)
        {
            this.ResponseType = responseType;
            this.Message = message;
        }

        /// <summary>
        /// Gets the message
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the response type
        /// </summary>
        public ServerResponseType ResponseType { get; private set; }

        /// <summary>
        /// Gets the response type as a string
        /// </summary>
        public string Response => this.ResponseType.ToString();
    }
}
using System;

namespace Diplo.GodMode.Models
{
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

        public ServerResponseType ResponseType { get; private set; }

        public string Response {  get { return this.ResponseType.ToString(); } }

        public string Message { get; private set; }
    }
}

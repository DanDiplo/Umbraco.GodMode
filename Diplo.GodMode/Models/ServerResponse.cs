using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplo.GodMode.Models
{
    public class ServerResponse
    {
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

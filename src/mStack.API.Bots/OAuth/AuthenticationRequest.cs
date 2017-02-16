using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.OAuth
{
    public abstract class AuthenticationRequest
    {
        private AuthenticationRequestType _requestType;
        private string _prompt = "Please click to sign in: ";
        private string _resourceId;
        private string[] _scopes;

        public AuthenticationRequest(AuthenticationRequestType type)
        {
            this._requestType = type;
        }

        public string ResourceId
        {
            get
            {
                return _resourceId;
            }
            set
            {
                _resourceId = value;
            }
        }

        public string[] Scopes
        {
            get
            {
                return _scopes;
            }
            set
            {
                _scopes = value;
            }
        }

        public string Prompt
        {
            get
            {
                return _prompt;
            }
            set
            {
                _prompt = value;
            }
        }

        public AuthenticationRequestType RequestType
        {
            get { return _requestType; }
        }
    }

    public enum AuthenticationRequestType
    {
        AzureADAL,
        AzureMSAL,
        AzureB2C,
        SharePointOnline
    }
}

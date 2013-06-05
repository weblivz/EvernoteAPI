using Evernote.EDAM.NoteStore;
using Evernote.EDAM.Type;
using Evernote.EDAM.UserStore;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Transport;

namespace org.livz.EvernoteAPIWrapper
{
    public class NotebookService
    {
        string _authToken = null;
        NoteService _NoteInstance = null;

        #if DEBUG
        private const string EVERNOTE_DOMAIN = "https://sandbox.evernote.com";
        #else
          private const string EVERNOTE_DOMAIN = "https://www.evernote.com";
        #endif

        public NotebookService(string authToken)
        {
            _authToken = authToken;
        }

        /// <summary>
        /// This will return a list of notebooks given a user token.
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public List<Notebook> GetNotebooks()
        {
            if (_NoteInstance == null)
            {
                _NoteInstance = new NoteService(_authToken);
            }

            // List all of the notebooks in the user's account        
            return _NoteInstance.Client.listNotebooks(_authToken);
        }
    }
}

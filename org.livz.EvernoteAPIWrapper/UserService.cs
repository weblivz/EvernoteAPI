﻿using Evernote.EDAM.NoteStore;
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
    public class UserService
    {
        UserStore.Client _Instance = null;
        NoteService _NoteInstance = null;
        string _authToken = null;

        #if DEBUG
        private string EVERNOTE_DOMAIN = "https://sandbox.evernote.com";
        #else
          private string EVERNOTE_DOMAIN = "https://www.evernote.com";
        #endif

        public UserService(string authToken)
        {
            _authToken = authToken;

            if (_Instance == null)
            {
                _Instance = CreateInstance();
            }
        }

        public UserStore.Client Client
        {
            get
            {
                return _Instance;
            }
        }

        /// <summary>
        /// Creates a new instance of a user client
        /// </summary>
        /// <returns></returns>
        UserStore.Client CreateInstance()
        {
            // override domain setting if we have one
            if (!String.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings["EVERNOTE_DOMAIN"])) EVERNOTE_DOMAIN = System.Configuration.ConfigurationManager.AppSettings["EVERNOTE_DOMAIN"];

            // first the user info
            Uri userStoreUrl = new Uri(EVERNOTE_DOMAIN + "/edam/user");
            TTransport userStoreTransport = new THttpClient(userStoreUrl);
            TProtocol userStoreProtocol = new TBinaryProtocol(userStoreTransport);
            return new UserStore.Client(userStoreProtocol);
        }

        public User Get(string authToken)
        {
            return _Instance.getUser(authToken);
        }

        /// <summary>
        /// Gets the current version set for the users updates. This is incremented at a global level each time a user updates any notebook.
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns>The global update count for this user. It is likely </returns>
        public int GetVersion()
        {
            if (_NoteInstance == null)
            {
                _NoteInstance = new NoteService(_authToken);
            }

            // now check the current state
            SyncState currentState = _NoteInstance.Client.getSyncState(_authToken);
            return currentState.UpdateCount; 
        }
    }
}

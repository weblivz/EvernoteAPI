﻿using Evernote.EDAM.NoteStore;
using Evernote.EDAM.Type;
using Evernote.EDAM.UserStore;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Thrift.Protocol;
using Thrift.Transport;

namespace org.livz.EvernoteAPIWrapper
{
    public enum TextResolver
    {
        raw,    // leave all markup
        basic,  // removes most markup
        strip   // removes all markup
    }

    public class NoteService
    {
        string _authToken = null;
        NoteStore.Client _Instance = null;
        UserService _UserInstance = null;

        #region templates
        
        // The templates representing an an Evernote note is represented using Evernote Markup Language
        // (ENML). The full ENML specification can be found in the Evernote API Overview
        // at http://dev.evernote.com/documentation/cloud/chapters/ENML.php


        const string TEMPLATE_BASIC_NOTE = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<!DOCTYPE en-note SYSTEM \"http://xml.evernote.com/pub/enml2.dtd\">" +
                "<en-note>{0}</en-note>";

        #endregion

        #if DEBUG
                private string EVERNOTE_DOMAIN = "https://sandbox.evernote.com";
        #else
                private string EVERNOTE_DOMAIN = "https://www.evernote.com";
        #endif

        public NoteService(string authToken)
        {
            _authToken = authToken;

            if (_Instance == null)
            {
                _Instance = CreateInstance(authToken);
            }
        }

        public NoteStore.Client Client
        {
            get
            {
                return _Instance;
            }
        }

        NoteStore.Client CreateInstance(string authToken)
        {
            // override domain setting if we have one
            if (!String.IsNullOrWhiteSpace(System.Configuration.ConfigurationManager.AppSettings["EVERNOTE_DOMAIN"])) EVERNOTE_DOMAIN = System.Configuration.ConfigurationManager.AppSettings["EVERNOTE_DOMAIN"];

            if (authToken != null)
            {
                // create a new user instance
                _UserInstance = new UserService(authToken);

                String noteStoreUrl = _UserInstance.Client.getNoteStoreUrl(authToken);

                // notebook info
                TTransport noteStoreTransport = new THttpClient(new Uri(noteStoreUrl));
                TProtocol noteStoreProtocol = new TBinaryProtocol(noteStoreTransport);
                return new NoteStore.Client(noteStoreProtocol);
            }

            return null;
        }

        /// <summary>
        /// Retrieves the note with the specified Guid and its content.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public NoteModel Get(Guid id, TextResolver resolver)
        {
            Note note = _Instance.getNote(_authToken, id.ToString(), true, false, false, false);

            return BuildNote(note, resolver);
        }

        /// <summary>
        /// Creates a new note on evernote.
        /// </summary>
        /// <param name="title"></param>
        public Note Create(string title, string content)
        {
            // creates the new note, sets the and content using the evernote format
            Note note = new Note();
            note.Title = title;
            note.Content = String.Format(TEMPLATE_BASIC_NOTE, content);

            // Send the new note to Evernote.
            Note newnote = _Instance.createNote(_authToken, note);

            // return the new note with the guid and update timestamp
            return newnote;
        }

        /// <summary>
        /// Updates a new note on evernote.
        /// </summary>
        /// <param name="title"></param>
        public Note Update(string guid, string title, string content)
        {
            // creates the new note, sets the and content using the evernote format
            Note note = new Note();
            note.Guid = guid;
            note.Title = title;
            note.Content = String.Format(TEMPLATE_BASIC_NOTE, content);

            // Send the new note to Evernote.
            Note newnote = _Instance.updateNote(_authToken, note);

            // return the new note with the guid and update timestamp
            return newnote;
        }

        /// <summary>
        /// This will return a list of notebooks given a user token.
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public List<Notebook> GetNotebooks()
        {
            // List all of the notebooks in the user's account        
            return _Instance.listNotebooks(_authToken);
        }

        /// <summary>
        /// This will read a set of annotated notes from the specified notebook. That is, this method will attempt to get the note, its attributes, the content and resolve images in the content.
        /// Using this call you'd likely have stored the version against each notebookid in your local data store.
        /// </summary>
        /// <param name="authToken">Your auth token to authenticate against the api.</param>
        /// <param name="notebookid">The Id of the notebook to filter on.</param>
        /// <param name="version">This is compared with the UpdateCount which is a value Evernote store at the account level to say if ANY of the notebooks have been updated. 
        /// You can get this from the UserService.GetVersion() method. The first call will always get the latest.</param>
        /// <param name="version">This is the timestamp of the last note you retrieved. The first call will get the latest notes.</param>
        /// <param name="raw">Html returns the full XML with much of the content resolved. Strip will return pure text. Basic will strip most of the XML but resolve images etc and leave basic HTML elements in there - useful is writing to a webpage.</param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public IList<NoteModel> GetNotes(Guid notebookid, long? timestamp, out long newtimestamp, TextResolver resolver = TextResolver.basic)
        {
            // initialize
            newtimestamp = 0;

            if (!timestamp.HasValue) timestamp = 0;

            // add in a filter
            int pageSize = 10;
            int pageNumber = 0;
            NoteFilter filter = new NoteFilter();
            filter.Order = (int)Evernote.EDAM.Type.NoteSortOrder.UPDATED;
            filter.Ascending = false;
            filter.NotebookGuid = notebookid.ToString(); // set the notebook to filter on

            // what do we want back from the query?
            //NotesMetadataResultSpec resultSpec = new NotesMetadataResultSpec() { IncludeTitle = true, IncludeUpdated = true };

            // execute the query for the notes
            NoteList newNotes = _Instance.findNotes(_authToken, filter, pageNumber * pageSize, pageSize);//, resultSpec);
            
            // initialize response collection
            IList<NoteModel> notes = new List<NoteModel>();

            // store the latest timestamp
            if (newNotes.Notes != null && newNotes.Notes.Count > 0)
            {
                newtimestamp = newNotes.Notes.FirstOrDefault().Updated;
            }

            // enumerate and build response
            foreach (Note note in newNotes.Notes)
            {
                // if the db timestamp is the same or later than the note then ignore it
                // is the timestamp which is the last time this project was checked
                if (timestamp >= note.Updated)
                {
                    continue;
                }
                
                notes.Add(BuildNote(note, resolver));
            }

            return notes;
        }

        /// <summary>
        /// Builds a note that can be displayed within Viq.
        /// </summary>
        /// <param name="note"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        private NoteModel BuildNote(Note note, TextResolver resolver)
        {
            // we have a note so we need to get the full content - we will extract the text for now and maybe images
            string content = _Instance.getNoteContent(_authToken, note.Guid);

            // we have a new note - etl the data
            NoteModel newnote = new NoteModel();
            newnote.ExternalId = new Guid(note.Guid);
            newnote.Title = note.Title;
            newnote.Timestamp = note.Updated;
            newnote.Text = ParseContent(note, content, resolver);
            newnote.DateCreated = DateTime.Now;
            newnote.DateModified = DateTime.Now;
            newnote.Notebook = new Guid(note.NotebookGuid);

            return newnote;
        }

        /// <summary>
        /// Takes the raw evernote output and transforms to something we can consume.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        private string ParseContent(Note note, string content, TextResolver resolver)
        {
            switch (resolver)
            {
                case TextResolver.basic :
                    {
                        return BasicContent(note, content);
                    }
                case TextResolver.strip :
                    {
                        return StripContent(content);
                    }
                default: {

                    return content;
                }
            }
        }

        #region remove markup

        /// <summary>
        /// This returns a basic html view over the content. It removes most of the Evernote formatting
        /// and is useful for local display and editing. If you just want text, look at StripContent();
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public string BasicContent(string content)
        {
            return BasicContent(null, content);
        }

        private string BasicContent(Note note, string content)
        {
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(content);
            
            // clean stuff up
            RemoveEmptyDivElements(doc);
            RemoveStyleAttributes(doc);
            ResolveMedia(note, doc);

            // get the html for the document
            var node = doc.DocumentNode.SelectSingleNode("en-note");
            if (node != null)
                content = doc.DocumentNode.SelectSingleNode("en-note").InnerHtml;
            else
                content = doc.DocumentNode.InnerHtml;

            // remove any leading or trailing whitespace
            return content.Trim();
        }

        /// <summary>
        /// Ommitted for now - we actually need to make an authenticated http POST to get each resource.
        /// Will do this shortly. More info http://dev.evernote.com/start/core/resources.php
        /// </summary>
        /// <param name="note"></param>
        /// <param name="html"></param>
        void ResolveMedia(Note note, HtmlAgilityPack.HtmlDocument html)
        {
            // TODO: We are removing these just now but we really want to download and locally store them via http POST
            RemoveMedia(html);
            return;

            if (note == null) return;

            //<en-media alt="Penultimate" type="image/png" hash="bb54c12582d7d1793fb860ae27fe9daa"></en-media>
            var els = html.DocumentNode.SelectNodes("//en-media");

            if (els != null)
            {
                foreach (var element in els)
                {
                    // try to load in the image given the hash
                    Resource img = _Instance.getResourceByHash(_authToken, note.Guid, System.Text.Encoding.Unicode.GetBytes(element.GetAttributeValue("hash", String.Empty)), true, false, false);
                    
                    // make sure we got something back and it has a url
                    if (img == null || img.Attributes == null || !String.IsNullOrWhiteSpace(img.Attributes.SourceURL)) return;

                    // get the url
                    string url = img.Attributes.SourceURL;

                    #region convert image to a local image
                    /*
                    System.Drawing.ImageConverter converter = new System.Drawing.ImageConverter();
                    System.Drawing.Image imgres = (System.Drawing.Image)converter.ConvertTo(img.Data.Body, typeof(System.Drawing.Image));
                     
                     // If we want to put the image in we need to somehow convert the image to base 64 encoding as we are not referencing the url explicitly                     
                    */
                    #endregion

                    // set the image url
                    HtmlAgilityPack.HtmlNode replacementImage = new HtmlAgilityPack.HtmlNode(HtmlAgilityPack.HtmlNodeType.Element, element.OwnerDocument, 0);
                    replacementImage.Attributes.Add("src", url);
                    
                    // we can now replace the original media tag with the new one
                    element.ParentNode.ReplaceChild(replacementImage, element);
                }
            }
        }

        void RemoveMedia(HtmlAgilityPack.HtmlDocument html)
        {
            var els = html.DocumentNode.SelectNodes("//en-media");

            if (els != null)
            {
                foreach (var element in els)
                {
                    element.Remove();
                }
            }
        }

        void RemoveEmptyDivElements(HtmlAgilityPack.HtmlDocument html)
        {
            var els = html.DocumentNode.SelectNodes("//div[text()='&nbsp;']");

            if (els != null)
            {
                foreach (var element in els)
                {
                    element.ParentNode.RemoveChild(element);
                }
            }
        }

        void RemoveStyleAttributes(HtmlAgilityPack.HtmlDocument html)
        {
            var els = html.DocumentNode.SelectNodes("//@style");

            if (els != null)
            {
                foreach (var element in els)
                {
                    element.Attributes["style"].Remove();
                }
            }
        }

        #endregion

        /// <summary>
        /// Loads the content as Xml and just return the text content with newlines preserved.
        /// This removes images and so on. Just inline text content. It adds whitespace padding where it makes sense.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public string StripContent(string content)
        {            
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(content);
            
            // remove evernote newlines and use our own
            doc.DocumentNode.InnerHtml = Regex.Replace(doc.DocumentNode.InnerHtml, @"<br[^>]*>", "\r\n\r\n");

            // get just the text content
            content = doc.DocumentNode.InnerText.Replace("&nbsp;", " ").Replace("&amp;", "&");

            // remove any remaining markup, including DTD's and so on
            content = Regex.Replace(content, @"<[^>]*>", String.Empty);

            // turn multiple newlines into one & clean whitespace formatting
            content = Regex.Replace(content, "[\r\n]\\s+[\r\n]", "\r\n\r\n");

            // remove any leading or trailing whitespace
            return content.Trim();
        }
    }
}

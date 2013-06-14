using org.livz.EvernoteAPIWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NUnit.Framework;

namespace org.livz.EvernoteAPIWrapperTest
{
    class Program
    {
        // get a developer token for the sandox from here https://sandbox.evernote.com/api/DeveloperToken.action
        static string authToken = "S=s1:U=6b313:E=1463d6defca:C=13ee5bcc3cd:P=1cd:A=en-devtoken:V=2:H=b2673c69d543c25736e89e7880fa26ad";

        static void Main(string[] args)
        {
            #if DEBUG            
            #else
            Console.WriteLine("In release mode remember you need to have a token for the production box - your sandbox tokens will not work. You can override by adding an EVERNOTE_DOMAIN TO THE appSettings in your web.config. E.g. <add key=\"EVERNOTE_DOMAIN\" value=\"https://sandbox.evernote.com\" />");
            #endif

            RunContentParsingTest();
            Guid id = RunCreateNoteTest();
            RunGetNoteTest(id);
        }

        [Test]
        static void RunContentParsingTest()
        {
            NoteService svc = new NoteService(authToken);
            string content = File.ReadAllText("sample.xml");
            //Console.WriteLine(svc.StripContent(content));

            string parsedContent = svc.BasicContent(content);

            Assert.IsNotNullOrEmpty(parsedContent);

            Console.WriteLine(parsedContent);
            Console.ReadKey();
        }

        [Test]
        static Guid RunCreateNoteTest()
        {
            NoteService svc = new NoteService(authToken);

            // create a new note
            Evernote.EDAM.Type.Note note = svc.Create("Test Note", "Hello World. I am a test note");

            Guid id = new Guid(note.Guid);

            Assert.IsNotNull(id);

            Console.WriteLine("Create new note with id " + id.ToString() + " and timestamp" + note.Updated + " and seq " + note.UpdateSequenceNum);
            Console.ReadKey();

            return id;
        }

        [Test]
        static void RunGetNoteTest(Guid id)
        {
            NoteService svc = new NoteService(authToken);

            // get back the note
            NoteModel note = svc.Get(id, TextResolver.raw);

            Assert.IsNotNull(note);
            Assert.AreEqual(note.Title, "Test Note");
            Assert.AreEqual(note.Text, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><!DOCTYPE en-note SYSTEM \"http://xml.evernote.com/pub/enml2.dtd\"><en-note>Hello World. I am a test note</en-note>");

            Console.WriteLine("Got note " + note.Title);
            Console.WriteLine(note.Text);
            Console.ReadKey();

        }
    }
}

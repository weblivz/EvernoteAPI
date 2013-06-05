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
        static string authToken = "YOUR TOKEN";

        static void Main(string[] args)
        {
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

            string parsedContent = svc.BasicContent(null, content);

            Assert.IsNotNullOrEmpty(parsedContent);

            Console.WriteLine(parsedContent);
            Console.ReadKey();
        }

        [Test]
        static Guid RunCreateNoteTest()
        {
            NoteService svc = new NoteService(authToken);

            // create a new note
            Guid id = svc.Create("Test Note", "Hello World. I am a test note");

            Assert.IsNotNull(id);

            Console.WriteLine("Create new note with id " + id.ToString());
            Console.ReadKey();

            return id;
        }

        [Test]
        static void RunGetNoteTest(Guid id)
        {
            NoteService svc = new NoteService(authToken);

            // get back the note
            Evernote.EDAM.Type.Note note = svc.Get(id);

            Assert.IsNotNull(note);
            Assert.AreEqual(note.Title, "Test Note");
            Assert.AreEqual(note.Content, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><!DOCTYPE en-note SYSTEM \"http://xml.evernote.com/pub/enml2.dtd\"><en-note>Hello World. I am a test note</en-note>");

            Console.WriteLine("Got note " + note.Title);
            Console.WriteLine(note.Content);
            Console.ReadKey();

        }
    }
}

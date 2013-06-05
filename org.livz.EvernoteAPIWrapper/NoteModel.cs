using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.livz.EvernoteAPIWrapper
{
    public class NoteModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public bool IsPublic { get; set; }
        public System.DateTime DateCreated { get; set; }
        public System.DateTime DateModified { get; set; }
    }
}

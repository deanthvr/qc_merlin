using System;

namespace AppMerlin
{

  //Please don't edit notes directly; instead use Edit() method which will update other fields appropriately.
  //Couldn't make fields private due to this class needing to be serialized. 
  public class Note
  {
    public string ID;
    public NoteType m_Type;
    public string text;
    public DateTime timeStamp;
    public string author;
    //public bool m_HasBeenEdited;

    public string LatestNote
    {
      get
      {
        return text;

      }
    }

    public DateTime LatestTimeStamp
    {
      get
      {
        return timeStamp;

      }
    }

    public string LatestAuthor
    {
      get
      {
        return author;

      }
    }

    /// <summary>
    /// Required for serialization. DON'T call this constructor.
    /// </summary>
    public Note()
    {

    }

    /// <summary>
    /// Creates a new Merlin note.
    /// </summary>
    /// <param name="type">The type of note.</param>
    /// <param name="note">The text of the note.</param>
    public Note(NoteType type, string note, string author)
    {
      ID = "";
      text = note;
      this.author = author;
      timeStamp = DateTime.Now;
      m_Type = type;
    }

    public Note(string id, NoteType type, string note, string author)
    {
      ID = id;
      text = note;
      this.author = author;
      timeStamp = DateTime.Now;
      m_Type = type;
    }

    public override bool Equals(object obj)
    {
      if (ID == null || ID == "")
      {
        return base.Equals(obj);
      }
      else
      {
        return ID.Equals(((Note)obj).ID);
      }
    }

    public override int GetHashCode()
    {
      if (ID == null || ID == "")
      {
        return base.GetHashCode();
      }
      else
      {
        return ID.GetHashCode();
      }
    }


    /// <summary>
    /// Edits the note.
    /// </summary>
    /// <param name="updatedNote">Updated note text.</param>
    public void Edit(string updatedNote, string author)
    {
      text = updatedNote;
      this.author = author;
      timeStamp = DateTime.Now;
    }
  }
}

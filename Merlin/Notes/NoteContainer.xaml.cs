using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using UserControl = System.Windows.Controls.UserControl;
using AppMerlin;

namespace Merlin.Notes
{
  /// <summary>
  /// Interaction logic for NoteContainer.xaml
  /// </summary>
  public partial class NoteContainer : UserControl
  {
    public List<Note> Notes;
    public NoteType Type;
    private string user;

    Style HeaderStyle
    {
      get
      {
        switch (Type)
        {
          case NoteType.ProjectLevel:
          case NoteType.IntersectionLevel:
            return (Style)FindResource("SectionSubHeaders");
          default:
            return (Style)FindResource("SectionSubSubHeaders");
        }
      }
    }

    public NoteContainer(ref List<Note> notes, NoteType type, string headerText, string currentUser)
    {
      Notes = notes;
      Type = type;
      user = currentUser;
      InitializeComponent();
      HeaderBlock.DataContext = this;
      PrepareForTypeAndLocation(headerText);
      AddHandler(NoteBullet.Note_Delete, new RoutedEventHandler(NoteDelete_Event));
      AddHandler(NoteBullet.Note_Refresh, new RoutedEventHandler(NoteRefresh_Event));
      PopulateNoteList();
    }

    private void NoteDelete_Event(object sender, RoutedEventArgs e)
    {
      NoteBullet noteBullet = (NoteBullet)e.OriginalSource;
      Notes.Remove(noteBullet.m_Note);
      PopulateNoteList();
    }

    private void NoteRefresh_Event(object sender, RoutedEventArgs e)
    {
      NoteBullet noteBullet = (NoteBullet)e.OriginalSource;
      NoteBullet editedNoteBullet = new NoteBullet(noteBullet.m_Note, user, Type == NoteType.CountLevel_Conglomerate ? true : false);
      int index = NotesBox.Items.IndexOf(noteBullet);
      NotesBox.Items.Insert(index, editedNoteBullet);
      NotesBox.Items.Remove(noteBullet);
      NotesBox.Items.Refresh();
    }

    private void submitButton_Click(object sender, RoutedEventArgs e)
    {
      if (!String.IsNullOrEmpty(InputBox.Text))
      {
        Note newNote = new Note(Type, InputBox.Text, user);
        Notes.Add(newNote);
        NoteBullet newBullet = new NoteBullet(newNote, user, Type == NoteType.CountLevel_Conglomerate ? true : false);
        NotesBox.Items.Add(newBullet);
        NotesBox.Items.Refresh();

        InputBox.Text = "";
      }
    }

    private void InputBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      SubmitButton.IsDefault = true;
    }

    private void InputBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      SubmitButton.IsDefault = false;
    }

    public void PopulateNoteList()
    {
      NotesBox.Items.Clear();
      foreach (Note note in Notes)
      {
        if (Type == NoteType.CountLevel_Conglomerate || note.m_Type == Type)
        {
          NoteBullet nb = new NoteBullet(note, user, Type == NoteType.CountLevel_Conglomerate ? true : false);
          NotesBox.Items.Add(nb);
        }
      }
    }

    public void PrepareForTypeAndLocation(string headerText)
    {
      HeaderBlock.Text = headerText;
      HeaderBlock.Style = HeaderStyle;
      switch (Type)
      {
        case NoteType.ProjectLevel:
          SubmitButton.ToolTip = "Submit Note for Project";
          HeaderBlock.Visibility = Visibility.Collapsed;
          break;
        case NoteType.IntersectionLevel:
          SubmitButton.ToolTip = "Submit Note for " + headerText;
          break;
        case NoteType.CountLevel_Conglomerate:
          SubmitButton.Visibility = Visibility.Collapsed;
          InputBox.Visibility = Visibility.Collapsed;
          break;
        default:
          SubmitButton.ToolTip = "Submit Note for " + headerText;
          break;
      }
    }



  }
}

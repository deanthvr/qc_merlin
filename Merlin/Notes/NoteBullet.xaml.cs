using System.Windows;
using AppMerlin;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows.Input;

namespace Merlin.Notes
{
  /// <summary>
  /// Interaction logic for NoteBullet.xaml
  /// </summary>
  public partial class NoteBullet : UserControl
  {
    public Note m_Note;
    private string user;
    private bool onNotesTab;
    private bool isEditing;
    public static readonly RoutedEvent Note_Delete =
      EventManager.RegisterRoutedEvent("Note_Delete", RoutingStrategy.Bubble,
      typeof(RoutedEventHandler), typeof(NoteBullet));
    public static readonly RoutedEvent Note_Refresh =
      EventManager.RegisterRoutedEvent("Note_Refresh", RoutingStrategy.Bubble,
      typeof(RoutedEventHandler), typeof(NoteBullet));
    public RoutedCommand noteDoubleClick;

    public string NoteText
    {
      get
      {
        return m_Note.LatestNote;
      }
    }

    public string NoteAuthor
    {
      get
      {
        return m_Note.LatestAuthor;
      }
    }

    public string NoteType
    {
      get
      {
        switch (m_Note.m_Type)
        {
          case AppMerlin.NoteType.ProjectLevel:
          case AppMerlin.NoteType.IntersectionLevel:
            return "";
          case AppMerlin.NoteType.CountLevel_Data:
            return "[Data]";
          case AppMerlin.NoteType.CountLevel_Balancing:
            return "[Balancing]";
          default:
            return "[Flag]";
        }
      }
    }

    public Visibility TypeBoxVisibility
    {
      get
      {
        switch (m_Note.m_Type)
        {
          case AppMerlin.NoteType.ProjectLevel:
          case AppMerlin.NoteType.IntersectionLevel:
            return Visibility.Collapsed;
          default:
            if (!onNotesTab)
            {
              return Visibility.Collapsed;
            }
            else
            {
              return Visibility.Visible;
            }
        }
      }
    }

    public string TimeStamp
    {
      get
      {
        return m_Note.LatestTimeStamp.ToShortDateString() + "   " + m_Note.LatestTimeStamp.TimeOfDay.ToString().Remove(5);
      }
    }

    public NoteBullet(Note note, string currentUser, bool isOnNotesTab)
    {
      m_Note = note;
      user = currentUser;
      onNotesTab = isOnNotesTab;
      InitializeComponent();
      noteBox.Visibility = Visibility.Collapsed;
      author.DataContext = this;
      timestamp.DataContext = this;
      noteBlock.DataContext = this;
      noteBox.DataContext = this;
      noteType.DataContext = this;

      noteDoubleClick = new RoutedCommand();
      noteDoubleClick.InputGestures.Add(new MouseGesture(MouseAction.LeftDoubleClick));
      CommandBinding noteDeleteCommand = new CommandBinding(noteDoubleClick, Note_DoubleClick);
      CommandBindings.Add(noteDeleteCommand);
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
      EditNote();
    }

    private void Note_DoubleClick(object sender, ExecutedRoutedEventArgs e)
    {
      EditNote();
    }
    
    private void EditNote()
    {
      if (!isEditing)
      {
        isEditing = true;
        noteBlock.Visibility = Visibility.Collapsed;
        noteBox.Visibility = Visibility.Visible;
        editButton.IsEnabled = false;
        deleteButton.IsEnabled = false;
        notePanel.UpdateLayout();
        noteBox.CaretIndex = noteBox.Text.Length;
        noteBox.Focus();
        Keyboard.Focus(noteBox);
      }
      else
      { }
    }

    private void EditNoteComplete()
    {
      if (isEditing)
      {
        m_Note.Edit(noteBox.Text, user);
        noteBlock.Visibility = Visibility.Visible;
        noteBox.Visibility = Visibility.Collapsed;
        editButton.IsEnabled = true;
        deleteButton.IsEnabled = true;
        notePanel.UpdateLayout();
        isEditing = false;
        RaiseEvent(new RoutedEventArgs(Note_Refresh));
      }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
      var result = MessageBox.Show("Are you sure you want to delete this note?", "Warning",
       MessageBoxButton.OKCancel);
      if (result == MessageBoxResult.OK)
      {
        RaiseEvent(new RoutedEventArgs(Note_Delete));
      }
    }

    private void noteBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      EditNoteComplete();
    }

    private void noteBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter || e.Key == Key.Return)
      {
        EditNoteComplete();
      }
    }

  }
}

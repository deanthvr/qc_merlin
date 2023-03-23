using System.Globalization;
using System.Linq;
using AppMerlin;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using Orientation = System.Windows.Controls.Orientation;
using PrintDialog = System.Windows.Controls.PrintDialog;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace Merlin.Printing
{
  public class MerlinPrintDocument
  {
    private const double pageHeightMargin = 84;
    private const double pageWidthMargin = 96;

    private TMCProject project;
    private FixedDocument document;
    private PrintDialog printDialog;
    private FixedPage currentPage;
    private double remainingPageSpace;
    private StackPanel currentPanel;
    private int currentPageNumber;

    public MerlinPrintDocument(TMCProject proj)
    {
      project = proj;
      document = new FixedDocument();
      printDialog = new PrintDialog();
    }

    public MerlinPrintDocument()
    {
      document = new FixedDocument();
      printDialog = new PrintDialog();
    }

    #region Notes Printing (FixedDocument Style)

    private void SetupDocument()
    {
      currentPageNumber = 0;
      currentPage = MakeNewPage();
      remainingPageSpace = currentPage.Height - pageHeightMargin;
      currentPanel = new StackPanel { Orientation = Orientation.Vertical };
      InsertHeader();
    }

    private FixedPage MakeNewPage()
    {
      FixedPage page = new FixedPage();
      page.Width = printDialog.PrintableAreaWidth;
      page.Height = printDialog.PrintableAreaHeight;
      //page.Width = document.DocumentPaginator.PageSize.Width;
      //page.Height = document.DocumentPaginator.PageSize.Height;
      page.Margin = new Thickness(48, 24, 48, 24);
      currentPageNumber++;
      return page;
    }

    private void AttachCurrentPage()
    {
      Grid pageNumberGrid = new Grid
      {
        Height = currentPage.Height - 60,
        Width = currentPage.Width - 96
      };
      TextBlock pageNumber = new TextBlock
      {
        Text = "Page " + currentPageNumber,
        VerticalAlignment = VerticalAlignment.Bottom,
        HorizontalAlignment = HorizontalAlignment.Right,
        Foreground = new SolidColorBrush(Colors.DarkGray),
        Margin = new Thickness(10, 0, 0, 0)
      };
      Image merlinLogo = new Image
      {
        Width = 16,
        Height = 16,
        VerticalAlignment = VerticalAlignment.Bottom,
        HorizontalAlignment = HorizontalAlignment.Right,
      };
      var mUri = new Uri("pack://application:,,,/Merlin;component/Resources/Sample Logo/merlin-light-blue-512x512.png", UriKind.RelativeOrAbsolute);
      merlinLogo.Source = new BitmapImage(mUri);
      StackPanel pageNumberPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
      pageNumberPanel.Children.Add(merlinLogo);
      pageNumberPanel.Children.Add(pageNumber);
      pageNumberGrid.Children.Add(pageNumberPanel);

      currentPage.Children.Add(currentPanel);
      currentPage.Children.Add(pageNumberGrid);
      PageContent pageContent = new PageContent();
      ((IAddChild)pageContent).AddChild(currentPage);
      document.Pages.Add(pageContent);
    }

    private void AddToCurrentPanel(FrameworkElement thingToAdd)
    {
      if (!(thingToAdd.Height > 0))
      {
        throw new Exception("The height property must be explicitly set in order to add it to the document.");
      }
      double thingsHeight = thingToAdd.Height + thingToAdd.Margin.Top + thingToAdd.Margin.Bottom;
      //if (thingToAdd.GetType() == typeof(TextBlock))
      //{
      //  thingsHeight += 2;
      //}

      if (remainingPageSpace < thingsHeight)
      {
        AttachCurrentPage();
        currentPage = MakeNewPage();
        remainingPageSpace = currentPage.Height - pageHeightMargin;
        currentPanel = new StackPanel { Orientation = Orientation.Vertical };
        InsertHeader();
      }

      currentPanel.Children.Add(thingToAdd);
      remainingPageSpace -= thingsHeight;

    }

    private void InsertHeader()
    {
      Image qcLogo = new Image();
      var qcUri = new Uri("pack://application:,,,/Merlin;component/Resources/QCLogoBlueRed.png", UriKind.RelativeOrAbsolute);
      qcLogo.Source = new BitmapImage(qcUri);
      qcLogo.Width = 144;
      qcLogo.Height = 48;

      //Image merlinLogo = new Image();
      //var mUri = new Uri("pack://application:,,,/Merlin;component/Resources/Sample Logo/merlin-light-blue-512x512.png", UriKind.RelativeOrAbsolute);
      //merlinLogo.Source = new BitmapImage(mUri);
      //merlinLogo.Width = 64;
      //merlinLogo.Height = 64;

      StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal };
      panel.Height = qcLogo.Height;
      panel.Children.Add(qcLogo);
      panel.Children.Add(new Grid
      {
        Width = currentPage.Width - 96 - qcLogo.Width
      });
      //panel.Children.Add(merlinLogo);

      AddToCurrentPanel(panel);
      AddToCurrentPanel(new Grid { Height = 20 });
      //StackPanel separatorPanel = new StackPanel { Orientation = Orientation.Vertical, Height = 4 };
      //separatorPanel.Children.Add(new Separator());
      //AddToCurrentPanel(separatorPanel);
    }

    private void AddAllNotes()
    {
      TextBlock mainHeaderBlock =
        CreateHeaderBlock(project.m_OrderNumber + " - " + project.m_ProjectName,
          (Style)Application.Current.FindResource("PrintingMainHeaders"));
      AddToCurrentPanel(mainHeaderBlock);
      TextBlock projNotesHeader =
        CreateHeaderBlock("Project Level Notes",
        (Style)Application.Current.FindResource("PrintingSectionHeaders"));
      AddToCurrentPanel(projNotesHeader);

      foreach (Note note in project.m_Notes)
      {
        AddToCurrentPanel(AddNote(note, false));
      }
      StackPanel projectSeparatorPanel = new StackPanel { Orientation = Orientation.Vertical, Height = 4, Margin = new Thickness(0, 6, 0, 0) };
      projectSeparatorPanel.Children.Add(new Rectangle { Height = 1, Width = double.NaN, Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 1 });
      AddToCurrentPanel(projectSeparatorPanel);

      TextBlock intNotesHeader =
        CreateHeaderBlock("Intersection and Count Level Notes",
        (Style)Application.Current.FindResource("PrintingSectionHeaders"));
      AddToCurrentPanel(intNotesHeader);

      foreach (Intersection intersection in project.m_Intersections)
      {
        TextBlock intersectionHeader =
          CreateHeaderBlock(intersection.GetLocationName() + " Notes:",
            (Style)Application.Current.FindResource("PrintingSectionSubHeaders"));

        AddToCurrentPanel(intersectionHeader);

        foreach (Note note in intersection.m_Notes)
        {
          AddToCurrentPanel(AddNote(note, false));
        }

        foreach (Count count in intersection.m_Counts)
        {
          if (count.m_Notes.Count > 0)
          {
            string headerString = count.m_Id + "   " + count.GetTimePeriod() + "   "
              + count.m_FilmDate.ToShortDateString();
            TextBlock countHeader = CreateHeaderBlock(headerString,
              (Style)Application.Current.FindResource("PrintingSectionSubSubHeaders"));

            AddToCurrentPanel(countHeader);

            foreach (Note note in count.m_Notes)
            {
              Flag flag = null;
              if (note.m_Type == NoteType.CountLevel_Flag)
              {
                flag = count.m_Flags.First(x => x.m_Key == note.ID);
              }
              AddToCurrentPanel(AddNote(note, true, flag));
            }
          }
        }
        StackPanel separatorPanel = new StackPanel { Orientation = Orientation.Vertical, Height = 4, Margin = new Thickness(0, 6, 0, 0) };
        separatorPanel.Children.Add(new Rectangle { Height = 1, Width = double.NaN, Stroke = new SolidColorBrush(Colors.DarkGray), StrokeThickness = 1 });
        AddToCurrentPanel(separatorPanel);
      }
    }

    private TextBlock CreateHeaderBlock(string text, Style style)
    {
      TextBlock header = new TextBlock();
      header.Text = text;
      header.Style = style;
      header.Height = 32;

      return header;
    }

    private StackPanel AddNote(Note note, bool showType, Flag flag = null)
    {
      double remainingWidth = currentPage.Width - pageWidthMargin;
      StackPanel notePanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Stretch,
        Margin = new Thickness(10, 0, 0, 0),
        Height = 20
      };
      if (note.m_Type != NoteType.ProjectLevel)
      {
        notePanel.Margin = new Thickness(20, 0, 0, 0);
      }
      remainingWidth -= (notePanel.Margin.Left + notePanel.Margin.Right);

      TextBlock authorBlock = new TextBlock
      {
        FontSize = 10,
        VerticalAlignment = VerticalAlignment.Center,
        Foreground = new SolidColorBrush(Colors.DarkSlateBlue),
        Width = 120,
        Margin = new Thickness(6, 0, 0, 0),
        Text = note.LatestAuthor
      };
      notePanel.Children.Add(authorBlock);

      remainingWidth -= (authorBlock.Width + authorBlock.Margin.Left + authorBlock.Margin.Right);
      //TextBlock timeBlock = new TextBlock
      //{
      //  FontSize = 10,
      //  VerticalAlignment = VerticalAlignment.Center,
      //  FontStyle = FontStyles.Italic,
      //  Width = 90,
      //  Margin = new Thickness(2, 0, 6, 0),
      //  Text = note.LatestTimeStamp.ToShortDateString() + "   " + note.LatestTimeStamp.TimeOfDay.ToString().Remove(5)
      //};
      //notePanel.Children.Add(timeBlock);

      if (showType)
      {
        string type;
        switch (note.m_Type)
        {
          case NoteType.ProjectLevel:
          case NoteType.IntersectionLevel:
            type = "";
            break;
          case NoteType.CountLevel_Data:
            type = "[Data]";
            break;
          case NoteType.CountLevel_Balancing:
            type = "[Balancing]";
            break;
          default:
            type = "[Flag]";
            break;
        }
        TextBlock typeBlock = new TextBlock
        {
          Width = double.NaN,
          VerticalAlignment = VerticalAlignment.Center,
          Margin = new Thickness(0, 0, 8, 0),
          Text = type
        };
        notePanel.Children.Add(typeBlock);

        var formattedTypeText = new FormattedText(
          typeBlock.Text, CultureInfo.CurrentUICulture,
          FlowDirection.LeftToRight,
          new Typeface(typeBlock.FontFamily, typeBlock.FontStyle, typeBlock.FontWeight, typeBlock.FontStretch),
          typeBlock.FontSize,
          Brushes.Black);

        remainingWidth -= (formattedTypeText.Width + typeBlock.Margin.Left + typeBlock.Margin.Right);
      }

      if (flag != null)
      {
        StackPanel flagPanel = new StackPanel { Orientation = Orientation.Horizontal };
        TextBlock flagTypeBlock = new TextBlock
        {
          FontSize = 10,
          Foreground = new SolidColorBrush(Colors.DarkOrange),
          VerticalAlignment = VerticalAlignment.Center,
          FontStyle = FontStyles.Italic,
          Width = 100,
          Margin = new Thickness(2, 0, 6, 0),
          Text = flag.m_Type.ToString()
        };
        TextBlock intervalBlock = new TextBlock
        {
          FontSize = 10,
          VerticalAlignment = VerticalAlignment.Center,
          FontStyle = FontStyles.Italic,
          Width = 40,
          Margin = new Thickness(2, 0, 6, 0),
          Text = flag.m_IntervalContainingError
        };
        if (String.IsNullOrEmpty(intervalBlock.Text))
        {
          intervalBlock.Text = "N/A";
        }
        TextBlock movementBlock = new TextBlock
        {
          FontSize = 10,
          VerticalAlignment = VerticalAlignment.Center,
          FontStyle = FontStyles.Italic,
          Width = 30,
          Margin = new Thickness(2, 0, 6, 0),
          Text = flag.m_Movement
        };
        flagPanel.Children.Add(flagTypeBlock);
        flagPanel.Children.Add(intervalBlock);
        flagPanel.Children.Add(movementBlock);
        notePanel.Children.Add(flagPanel);
        remainingWidth -= (flagTypeBlock.Width + flagTypeBlock.Margin.Left + flagTypeBlock.Margin.Right);
        remainingWidth -= (intervalBlock.Width + intervalBlock.Margin.Left + intervalBlock.Margin.Right);
        remainingWidth -= (movementBlock.Width + movementBlock.Margin.Left + movementBlock.Margin.Right);
      }

      TextBlock textBlock = new TextBlock
      {
        Width = remainingWidth,
        Height = double.NaN,
        VerticalAlignment = VerticalAlignment.Center,
        TextWrapping = TextWrapping.Wrap,
        Text = note.LatestNote
      };

      var formattedText = new FormattedText(
        textBlock.Text,
        CultureInfo.CurrentUICulture,
        FlowDirection.LeftToRight,
        new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
        textBlock.FontSize,
        Brushes.Black);
      if (formattedText.Width > remainingWidth)
      {
        int factor = (int)Math.Ceiling(formattedText.Width / remainingWidth);
        notePanel.Height = factor * 20;
      }

      //if (note.m_Type == NoteType.CountLevel_Flag && textBlock.Text.Length > 60)
      //{
      //  notePanel.Height = ((Int16)Math.Ceiling(textBlock.Text.Length / 75.0)) * 20;
      //  textBlock.Width = 400 - 194;
      //}
      //else if (textBlock.Text.Length > 75)
      //{
      //  notePanel.Height = ((Int16)Math.Ceiling(textBlock.Text.Length / 75.0)) * 20;

      //}

      notePanel.Children.Add(textBlock);

      return notePanel;
    }

    public void InsertNotes()
    {
      AddAllNotes();
      AttachCurrentPage();
    }

    public void PrintNotes()
    {
      if (printDialog.ShowDialog() == true)
      {
        SetupDocument();
        InsertNotes();
        string printName = project.m_OrderNumber + " - " + project.m_ProjectName + " Notes";
        var invalids = System.IO.Path.GetInvalidFileNameChars();
        var newName = String.Join("_", printName.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        newName = newName.Replace(".", " ");
        printDialog.PrintDocument(document.DocumentPaginator, newName);
        //foreach (var page in document.Pages)
        //{
        //  double pageSum = 0;
        //  double pageSumActual = 0;
        //  StackPanel mainPanel = page.Child.Children[0] as StackPanel;
        //  if (mainPanel != null)
        //  {
        //    foreach (var child in mainPanel.Children)
        //    {
        //      var child2 = child as FrameworkElement;
        //      if (child2 != null)
        //      {
        //        Console.WriteLine(child2.GetType() + ": " + child2.ActualHeight + child2.Margin.Top + child2.Margin.Bottom);
        //        pageSumActual += child2.ActualHeight;

        //        pageSum += child2.ActualHeight + child2.Margin.Top + child2.Margin.Bottom;
        //      }
        //    }
        //    Console.WriteLine("Current Panel Height: " + mainPanel.ActualHeight);
        //    Console.WriteLine("Page Sum with Margin Height: " + pageSum);
        //    Console.WriteLine("Page Sum Actual Height: " + pageSumActual);
        //  }
        //}
      }
    }

    #endregion

    #region DataTable Printing (FlowDocumentStyle)

    public void PrintDataSet(DataSet data, string countId, string location)
    {
      string dataHeader = countId + " - " + location;
      PrintDialog printDlg = new PrintDialog();

      FlowDocument fDoc = new FlowDocument();
      fDoc.PagePadding = new Thickness(50);
      fDoc.ColumnGap = 0;
      fDoc.ColumnWidth = printDlg.PrintableAreaWidth;
      Section docHeader = GetDocHeaderSection();
      fDoc.Blocks.Add(docHeader);
      for (int i = 0; i < data.Tables.Count; i++)
      {
        Section s = GetCountHeaderSection(dataHeader);
        if (i > 0)
        {
          s.BreakPageBefore = true;

        }
        fDoc.Blocks.Add(s);
        fDoc.Blocks.Add(GetTableHeader(data.Tables[i]));
        Section t = new Section();
        t.Blocks.Add(ConvertDataToTable(data.Tables[i]));
        t.FontFamily = new FontFamily("Consolas, Cambria");
        t.FontSize = 10;
        fDoc.Blocks.Add(t);
      }
      IDocumentPaginatorSource idpSource = fDoc;
      printDlg.ShowDialog();
      printDlg.PrintDocument(idpSource.DocumentPaginator, countId + "CountData");
    }

    public void PrintDataTable(DataTable table, string dataHeader)
    {

    }

    private Section GetDocHeaderSection()
    {
      Section section = new Section();
      Run line = new Run(project.m_OrderNumber + " - " + project.m_ProjectName);

      Run timeLine = new Run(DateTime.Now.ToLocalTime().ToString());
      timeLine.FontFamily = new FontFamily("Century Gothic, Times New Roman");
      timeLine.FontSize = 8;
      line.FontFamily = new FontFamily("Century Gothic, Times New Roman");
      line.FontSize = 18;

      Paragraph p1 = new Paragraph(timeLine);
      Paragraph p2 = new Paragraph(line);
      section.Blocks.Add(p1);
      section.Blocks.Add(p2);

      return section;
    }

    private Section GetCountHeaderSection(string dataHeader)
    {
      Section section = new Section();
      Paragraph headerParagraph = new Paragraph();
      Run line = new Run(dataHeader);
      line.FontFamily = new FontFamily("Times New Roman");
      headerParagraph.Inlines.Add(line);
      section.Blocks.Add(headerParagraph);
      return section;
    }

    private Section GetTableHeader(DataTable table)
    {
      Section section = new Section();
      Paragraph p1 = new Paragraph();
      Run line = new Run();
      line.FontFamily = new FontFamily("Times New Roman");
      string headerLine = table.TableName + ":  "
        + project.GetCombinedBankNames(int.Parse(table.TableName.Split(' ')[1]));
      line.Text = headerLine;
      p1.Inlines.Add(line);
      section.Blocks.Add(p1);
      return section;
    }

    private Table ConvertDataToTable(DataTable dataTable)
    {
      Table table = new Table();
      var rowGroup = new TableRowGroup();
      table.RowGroups.Add(rowGroup);
      var header = new TableRow();
      rowGroup.Rows.Add(header);

      foreach (DataColumn column in dataTable.Columns)
      {
        var tableColumn = new TableColumn();
        if (column.ColumnName == "Time")
        {
          tableColumn.Width = new GridLength(60);
        }
        else
        {
          tableColumn.Width = new GridLength(40);
        }
        table.Columns.Add(tableColumn);
        var cell = new TableCell(new Paragraph(new Run(column.ColumnName)));
        header.Cells.Add(cell);
      }

      foreach (DataRow row in dataTable.Rows)
      {
        var tableRow = new TableRow();
        rowGroup.Rows.Add(tableRow);

        foreach (DataColumn column in dataTable.Columns)
        {
          var value = row[column].ToString();
          var cell = new TableCell(new Paragraph(new Run(value)));
          tableRow.Cells.Add(cell);
        }
      }
      return table;
    }

    #endregion

  }
}

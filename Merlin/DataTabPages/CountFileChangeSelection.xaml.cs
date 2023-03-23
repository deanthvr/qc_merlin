using System;
using System.Windows;
using System.Windows.Controls;
using AppMerlin;

namespace Merlin
{
  /// <summary>
  /// Interaction logic for CountExportSelection.xaml
  /// </summary>
  public partial class CountFileChangeSelection : Window
  {
    private TMCProject project;
    public Count count;
    private string fileName;

    public CountFileChangeSelection(TMCProject proj, string file)
    {
      project = proj;
      fileName = file;
      InitializeComponent();
    }

    private void content_Rendered(object sender, EventArgs e)
    {
      countHeader.Text = fileName;
      foreach (Intersection intersection in project.m_Intersections)
      {
        foreach (Count count in intersection.m_Counts)
        {
          ListViewItem item = new ListViewItem();
          item.Content = count.m_Id + " - " + count.GetLocation();
          item.Tag = count;
          countListView.Items.Add(item);
        }
      }
    }

    private void cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }

    private void selectionComplete_Click(object sender, RoutedEventArgs e)
    {
      if (countListView.SelectedItems == null)
      {
        var result = MessageBox.Show("No Counts Selected, Ok to close?", "Close Window",
          MessageBoxButton.OKCancel);
        if (result == MessageBoxResult.Cancel)
        {
          return;
        }
        DialogResult = false;

      }
      count = (Count)((ListViewItem)countListView.SelectedItem).Tag;
      
      DialogResult = true;
    }

    private void CountList_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (countListView.SelectedItems == null)
      {
        DialogResult = false;
      }
      count = (Count)((ListViewItem)countListView.SelectedItem).Tag;
      DialogResult = true;
    }

  }
}

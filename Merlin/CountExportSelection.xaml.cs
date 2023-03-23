using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AppMerlin;

namespace Merlin
{
  /// <summary>
  /// Interaction logic for CountExportSelection.xaml
  /// </summary>
  public partial class CountExportSelection : Window
  {
    private TMCProject project;
    public List<Count> countsToExport;

    public CountExportSelection(TMCProject proj)
    {
      project = proj;
      countsToExport = new List<Count>();
      InitializeComponent();
    }

    private void content_Rendered(object sender, EventArgs e)
    {
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
        var result = MessageBox.Show("No Counts Selected, Ok to close?", "Close Export",
          MessageBoxButton.OKCancel);
        if (result == MessageBoxResult.Cancel)
        {
          return;
        }
        DialogResult = false;

      }
      foreach (var item in countListView.SelectedItems)
      {
        countsToExport.Add((Count)((ListViewItem)item).Tag);
      }

      DialogResult = true;
    }

  }
}

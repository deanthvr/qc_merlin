using AppMerlin;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Merlin.ExtensionClasses
{
  public class MerlinDataTableState
  {
    public Control Selection;
    public DataTable Table;
    public int Bank;
    public List<DataFile> FileAssocations;
    //public Dictionary<string, DataFile> CellMap;

    public MerlinDataTableState()
    {

    }

    public MerlinDataTableState(Control countInTree, DataTable dataState, int currentBank, List<DataFile> fileAssociation)
    {
      Selection = countInTree;
      Table = dataState.Copy();
      Bank = currentBank;
      FileAssocations = new List<DataFile>(fileAssociation.Select(x => (DataFile)x.Clone()));
      //CellMap = cellMap.ToDictionary(x => (string)x.Key.Clone(), x => (DataFile)x.Value.Clone());
    }

    public MerlinDataTableState(DataTable dataState, int currentBank)
    {
      Selection = null;
      Table = dataState.Copy();
      Bank = currentBank;
      FileAssocations = null;
      //CellMap = null;
    }


  }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using fxcore2;

namespace PrintTable
{
    class EachRowListener : IO2GEachRowListener
    {
        private string mAccountID;

        public EachRowListener(string sAccountID)
        {
            mAccountID = sAccountID;
        }

        public void onEachRow(string sRowID, O2GRow rowData)
        {
            if (rowData.TableType == O2GTableType.Orders)
            {
                int columnsCount = rowData.Columns.Count;
                for (int i = 0; i < columnsCount; i++)
                {
                    if (string.IsNullOrEmpty(mAccountID) || mAccountID.Equals(((O2GOrderTableRow)rowData).AccountID))
                    {
                        Console.Write(rowData.Columns[i].ID + "=" + rowData.getCell(i) + "; ");
                    }
                }
                Console.WriteLine("");
            }
        }
    }
}

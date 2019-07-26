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
            if (rowData.TableType == O2GTableType.Orders ||
                rowData.TableType == O2GTableType.Trades)
            {
                string accountID = "";
                if (rowData.TableType == O2GTableType.Orders)
                    accountID = ((O2GOrderTableRow)rowData).AccountID;
                else
                    accountID = ((O2GTradeTableRow)rowData).AccountID;

                int columnsCount = rowData.Columns.Count;
                for (int i = 0; i < columnsCount; i++)
                {
                    if (string.IsNullOrEmpty(mAccountID) || mAccountID.Equals(accountID))
                    {
                        Console.Write(rowData.Columns[i].ID + "=" + rowData.getCell(i) + "; ");
                    }
                }
                Console.WriteLine("");
            }
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBM.Data.DB2.iSeries;

namespace DB2readTest
{
    class Program
    {
        private static iDB2Connection DB2UdbConnection = new iDB2Connection();
        private static iDB2Connection conn_i5
        {
            get { return DB2UdbConnection; }
            set { DB2UdbConnection = value; }
        }
        private static string cmdItemWarehouse = 
            "SELECT ITNOIB, WHIDIB, UCAVIB, UCLSIB, QTOHIB, QALMIB, QALCIB, " + 
                "QALPIB, QPRLIB, QOOMIB, QOOPIB, IIOQM5, OIOQM5, QINTM5 " +
            "FROM ITEMBLL0 INNER JOIN ITEMBXL0 ON WHIDM5 = WHIDIB AND ITNOM5 = ITNOIB " +
            "WHERE ";
        //private static string cmdItemWarehouseFaulty =
        //    "SELECT ITNOIB, WHIDIB, UCAVIB, UCLSIB, QTOHIB, QALMIB, QALCIB, MAX(TRDAAV) " +
        //        "QALPIB, QPRLIB, QOOMIB, QOOPIB, IIOQM5, OIOQM5, QINTM5, TRDAAV " +
        //    "FROM ITEMBLL0 INNER JOIN ITEMBXL0 ON ITNOIB = ITNOM5 AND WHIDIB = WHIDM5 " +
        //    "INNER JOIN IMHISTL5 ON ITNOAV = ITNOM5 AND WHIDAV = WHIDM5 " +
        //    "WHERE WHIDIB = '9' AND (TCDEAV = 'PQ' OR TCDEAV = 'RP') AND ITNOIB = '";
        private static string cmdInventoryTransHistory =
            "SELECT MAX(TRDAAV) " +
            "FROM IMHISTL5 " +
            "WHERE (TCDEAV = 'PQ' OR TCDEAV = 'RP') AND ";
        private static string cmdInventoryTransHistoryList =
            "SELECT ITNOAV, WHIDAV, TRDAAV, TCDEAV " +
            "FROM IMHISTL5 " +
            "WHERE (TCDEAV = 'PQ' OR TCDEAV = 'RP') AND ";

        private static StreamWriter sw;

        static void Main(string[] args)
        {
            DateTime start_time;
            DateTime stop_time;
            TimeSpan elapsed_time;
            DataSet ds = new DataSet();
            string cmd;

            Console.WriteLine(" ---  DB2 database read test program, v1.1 (May, 2016) ---");
            Console.WriteLine();
            Console.WriteLine("                                      Autor: Zoran Gojčeta");
            Console.WriteLine();
            Console.WriteLine();

            try
            {
                if (ConnectToDB2())
                {
                    string dir = @"C:\Users\GOJCETAZ\Documents\DB2BOM_Ext\";
                    sw = new StreamWriter(dir + "DB2BOMextData.csv");
                    Console.WriteLine("ItemNbr\tWhs\tAverageUnitCost\tLastUnitCost\tTotalQuantityAvailable\tQuantityAvailableOnHand\tPostedDate");
                    sw.WriteLine("ItemNbr;Whs;AverageUnitCost;LastUnitCost;TotalQuantityAvailable;QuantityAvailableOnHand;PostedDate");
                    while (true)
                    {
                        Console.WriteLine();
                        Console.Write(" Upiši broj dijela :=? ");
                        string itmNbr = Console.ReadLine().ToUpper();
                        if (itmNbr.Length == 0)
                            break;
                        Console.Write(" Upiši broj skladišta (7 ili 9) :=? ");
                        string whsNbr = Console.ReadLine().ToUpper();

                        start_time = DateTime.Now;
                        cmd = cmdItemWarehouse + "ITNOIB = '" + itmNbr + "' AND WHIDIB = '" + whsNbr + "'";
                        if (!QryUpdDelCre(cmd, ref ds))
                            Console.WriteLine("Obavijest o grešci:\n" +
                                "\tNeuspješno čitanje podataka iz Item Warehouse !" +
                                "\tNaredba: \r\n\t" + cmd);
                        else
                        {
                            if (ds.Tables[0].Rows.Count == 0)
                            {
                                Console.WriteLine("Obavijest o grešci:\n" +
                                    "\tBroj dijela '" + itmNbr + "' nije pronađen !" +
                                    "\tNaredba: \r\n\t" + cmd);
                                break;
                            }
                            string ITNOIB = ds.Tables[0].Rows[0][0].ToString().Trim();
                            string WHIDIB = ds.Tables[0].Rows[0][1].ToString().Trim();
                            float UCAVIB = float.Parse(ds.Tables[0].Rows[0][2].ToString().Trim());
                            float UCLSIB = float.Parse(ds.Tables[0].Rows[0][3].ToString().Trim());
                            float QTOHIB = float.Parse(ds.Tables[0].Rows[0][4].ToString().Trim());
                            float QALMIB = float.Parse(ds.Tables[0].Rows[0][5].ToString().Trim());
                            float QALCIB = float.Parse(ds.Tables[0].Rows[0][6].ToString().Trim());
                            float QALPIB = float.Parse(ds.Tables[0].Rows[0][7].ToString().Trim());
                            float QPRLIB = float.Parse(ds.Tables[0].Rows[0][8].ToString().Trim());
                            float QOOMIB = float.Parse(ds.Tables[0].Rows[0][9].ToString().Trim());
                            float QOOPIB = float.Parse(ds.Tables[0].Rows[0][10].ToString().Trim());
                            float IIOQM5 = float.Parse(ds.Tables[0].Rows[0][11].ToString().Trim());
                            float OIOQM5 = float.Parse(ds.Tables[0].Rows[0][12].ToString().Trim());
                            float QINTM5 = float.Parse(ds.Tables[0].Rows[0][13].ToString().Trim());
                            string postedDate = string.Empty;

                            float QTAVIB = QTOHIB - (QALMIB + QALCIB + IIOQM5 + OIOQM5) - 
                                          (QALPIB + QPRLIB) + (QOOMIB + QOOPIB + QINTM5);
                            float QTAOIB = QTOHIB - (QALMIB + QALCIB + IIOQM5 + OIOQM5);

                            //string dir = @"C:\Users\GOJCETAZ\Documents\DB2BOM_Ext\";
                            //if (File.Exists(dir + itmNbr + ".csv"))
                            //    File.Delete(dir + itmNbr + ".csv");
                            //sw = new StreamWriter(dir + itmNbr + ".csv");
                            //Console.WriteLine("ItemNbr\tWhs\tAverageUnitCost\tLastUnitCost\tTotalQuantityAvailable\tQuantityAvailableOnHand\tPostedDate");
                            //Console.WriteLine(ITNOIB + "\t" + WHIDIB + "\t" + UCAVIB + "\t" + UCLSIB + "\t" + QTAVIB + "\t" + QTAOIB + "\t" + postedDate);
                            //sw.WriteLine("ItemNbr;Whs;AverageUnitCost;LastUnitCost;TotalQuantityAvailable;QuantityAvailableOnHand;PostedDate");
                            //sw.WriteLine(ITNOIB + ";" + WHIDIB + ";" + UCAVIB + ";" + UCLSIB + ";" + QTAVIB + ";" + QTAOIB + ";" + postedDate);

                            ds.Reset();
                            cmd = cmdInventoryTransHistory + "ITNOAV = '" + itmNbr + "' AND WHIDAV = '" + whsNbr + "'";
                            if (!QryUpdDelCre(cmd, ref ds))
                                Console.WriteLine("Obavijest o grešci:\n" +
                                    "\tNeuspješno čitanje podataka iz Inventory History !" +
                                    "\tNaredba: \r\n\t" + cmd);
                            else
                            {
                                //Console.WriteLine();
                                //Console.WriteLine("TransactionDate\tTransactioCode\t\t\t");
                                //sw.WriteLine();
                                //sw.WriteLine("TransactionDate;TransactioCode;;;");

                                if (ds.Tables[0].Rows.Count > 0)
                                {

                                    //if (ds.Tables[0].Rows.Count == 1)
                                    //{
                                        postedDate = ds.Tables[0].Rows[0][0].ToString().Trim();
                                        if (postedDate.Length == 7)
                                            postedDate = postedDate.Substring(5) + "." + postedDate.Substring(3, 2)
                                                 + "." + (postedDate.Substring(0, 1) == "0" ? "19" : "20") + postedDate.Substring(1, 2);
                                    Console.WriteLine(ITNOIB + "\t" + WHIDIB + "\t" + UCAVIB + "\t" + UCLSIB + "\t" + QTAVIB + "\t" + QTAOIB + "\t" + postedDate);
                                    sw.WriteLine(ITNOIB + ";" + WHIDIB + ";" + UCAVIB + ";" + UCLSIB + ";" + QTAVIB + ";" + QTAOIB + ";" + postedDate);
                                    //Console.WriteLine(postedDate + "\tPQ or RP\t\t\t");
                                    //sw.WriteLine(postedDate + ";PQ or RP;;;");
                                    //}
                                    //else
                                    //{
                                    //    foreach (DataRow dr in ds.Tables[0].Rows)
                                    //    {
                                    //        string TRDAAV = dr[2].ToString().Trim();
                                    //        string TCDEAV = dr[3].ToString().Trim();
                                    //        Console.WriteLine(TRDAAV + "\tPQ or RP\t\t\t");
                                    //        sw.WriteLine(TRDAAV + ";" + TCDEAV + ";;;");
                                    //    }
                                    //}
                                }
                            }

                            //sw.Close();
                            stop_time = DateTime.Now;
                            elapsed_time = stop_time.Subtract(start_time);
                            Console.WriteLine
                                ("Vrijeme obrade : " +
                                 elapsed_time.TotalSeconds.ToString("0.00") + " sekunde.");
                            //Console.WriteLine();
                            //Console.WriteLine("Vidi izlaznu datoteku => " + dir + itmNbr + ".csv");
                            ds.Reset();
                        }
                    }
                    Console.WriteLine();
                    Console.WriteLine("Vidi izlaznu datoteku => " + dir + "DB2BOMextData.csv");
                    sw.Close();
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine("Obavijest o grešci");
                Console.WriteLine(Ex.Message);
                Console.WriteLine("\nNaredba: \"" + cmdItemWarehouse + "\"\n");
            }
            Console.WriteLine("Press a key to exit ... ");
            Console.ReadLine();
        }


        public static bool QryUpdDelCre(string cmdExec, ref DataSet dsResponse)
        {
            iDB2DataReader DB2UdbDataReader;
            string mstrQueryObject = string.Empty;
            string mstrError;

            mstrQueryObject = cmdExec;
            sqlCommand DB2Cmd = new sqlCommand(mstrQueryObject);
            if (mstrQueryObject.Contains("SELECT "))
            {
                try
                {
                    DB2UdbDataReader = DB2Cmd.DB2UdbCommand.ExecuteReader();
                }
                catch (iDB2Exception e)
                {
                    mstrError = "Source: " + e.Source + Environment.NewLine +
                        "Message: " + e.Message + Environment.NewLine +
                        "MessageDetails: " + e.MessageDetails + Environment.NewLine +
                        "MessageCode: " + e.MessageCode + Environment.NewLine +
                        "SqlState: " + e.SqlState + Environment.NewLine +
                        "iDB2Exception type: " + e.GetType().ToString() +
                        Environment.NewLine + Environment.NewLine +
                        "There are " + e.Errors.Count.ToString() +
                        " errors in the error collection.";
                    Console.WriteLine(mstrError);
                    return false;
                }
                DataTable table = new DataTable();
                for (int i = 0; i <= DB2UdbDataReader.FieldCount - 1; i++)
                {
                    table.Columns.Add();
                }
                while (DB2UdbDataReader.Read())
                {
                    DataRow dr = table.NewRow();
                    for (int i = 0; i <= DB2UdbDataReader.FieldCount - 1; i++)
                    {
                        if (DB2UdbDataReader[i] != DBNull.Value)
                        {
                            dr[i] = DB2UdbDataReader.GetString(i);
                        }
                    }
                    table.Rows.Add(dr);
                }
                DB2UdbDataReader.Close();
                DB2UdbDataReader = null;
                dsResponse.Tables.Add(table);
            }
            return true;
        }


        public static bool ConnectToDB2()
        {
            DateTime start_time;
            DateTime stop_time;
            TimeSpan elapsed_time;
            string Connection =
                "DataSource=INFOR; ConnectionTimeout=60; UserID=GOJCETA; Password=GOJCETA; Database=DISTASP; DefaultCollection=AMFLIBA;";
            start_time = DateTime.Now;
            Console.WriteLine("Connecting to IBM DB2 database ...");
            if (Connect(Connection))
            {
                stop_time = DateTime.Now;
                elapsed_time = stop_time.Subtract(start_time);
                Console.WriteLine
                    ("Uspješna prijava na IBM DB2 bazu podataka." + Environment.NewLine +
                     "Vrijeme prijavljivanja : " +
                     elapsed_time.TotalSeconds.ToString("0.00") + " sekunde.");
                Console.WriteLine();
                return true;
            }
            else
            {
                return false;
            }
        }


        public static bool Connect(string Connection)
        {
            try
            {
                conn_i5 = new iDB2Connection(Connection);
                conn_i5.Open();
                return true;
            }
            catch (iDB2Exception e)
            {
                string Message = "Source: " + e.Source + Environment.NewLine +
                     "Message: " + e.Message + Environment.NewLine +
                     "MessageDetails: " + e.MessageDetails + Environment.NewLine +
                     "MessageCode: " + e.MessageCode + Environment.NewLine +
                     "SqlState: " + e.SqlState + Environment.NewLine +
                     "iDB2Exception type: " + e.GetType().ToString() +
                     Environment.NewLine + Environment.NewLine +
                     "There are " + e.Errors.Count.ToString() + " errors in the error collection.";
                Console.WriteLine("Obavijest o gresci");
                Console.WriteLine(Message);
                return false;
            }
        }


        private class sqlCommand
        {
            internal iDB2Command DB2UdbCommand = new iDB2Command();

            public sqlCommand(string sqlQuery)
            {
                DB2UdbCommand.Connection = conn_i5;
                DB2UdbCommand.CommandText = sqlQuery;
                DB2UdbCommand.CommandTimeout = 0;
                DB2UdbCommand.DeriveParameters();
            }
        }
    }
}

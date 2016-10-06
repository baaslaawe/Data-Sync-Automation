using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using System.Collections;

namespace LogService
{
    public static class LogDataServicing
    {
        static DataTable serverIDList = new DataTable();

        //Method for getting the output of the query
        public static string GetQueryOutput(string query, string serverName, string dbName,string queryName)
        {

            DataTable dataTable = null;
            
            foreach(DataRow serverID in serverIDList.Rows)
            {
                //Initialising a Sql Connection 
                SqlConnection oConn = new SqlConnection(@"Data Source=" + serverID["Server_Name"] + ";Database=Master;Integrated Security=SSPI");
                //Writing an SQL Query by passing in the connector and the query
                SqlCommand sc = new SqlCommand(query, oConn);
                //Open the Connection
                oConn.Open();
                //Fetching the data in the Sql DataReader
                SqlDataReader dr = sc.ExecuteReader();
                //Fetching the Data Table
                DataTable dt = new DataTable();
                //Loading the Table
                dt.Load(dr);

                oConn.Close();

                if (dt.Rows.Count != 0)
                {

                    if (dataTable == null)
                    {
                        dataTable = new DataTable();
                        dataTable = dt.Copy();
                    }
                    else
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            dataTable.Rows.Add(row);
                        }
                        
                    }

                    //Initialising a Sql Connection 
                    SqlConnection oConnection = new SqlConnection(@"Data Source=" + serverName + ";Database=" + dbName +";Integrated Security=SSPI");
                    //Writing an SQL Query by passing in the connector and the query
                    SqlCommand comm = new SqlCommand(query, oConnection);
                    //Open the Connection
                    oConnection.Open();
                

                    if (queryName.Contains("Multiple"))
                    {
                        comm.CommandText = "INSERT INTO Schema.T_Multiple_Sessions VALUES ";
                        foreach (DataRow row in dt.Rows)
                        {
                            comm.CommandText = comm.CommandText + "(NEWID(),CONVERT(uniqueidentifier,'" + serverID["FS_UID"] + "'),'" + row["login_name"] + "','" + row["session_count"] + "',CURRENT_TIMESTAMP),";
                        }
                        comm.CommandText = comm.CommandText.Remove(comm.CommandText.Length - 1);
                        comm.ExecuteNonQuery();
                    }
                    else if (queryName.Contains("Temp"))
                    {
                        comm.CommandText = "INSERT INTO Schema.T_TempDB_Size VALUES ";
                        foreach (DataRow row in dt.Rows)
                        {
                            comm.CommandText = comm.CommandText + "(NEWID(),CONVERT(uniqueidentifier,'" + serverID["FS_UID"] + "'),'" + row["FileName"] + "','" + row["FileSizeinMB"] + "','" + row["GrowthValue"] + "',CURRENT_TIMESTAMP),";
                        }
                        comm.CommandText = comm.CommandText.Remove(comm.CommandText.Length - 1);
                        comm.ExecuteNonQuery();
                    }
                    else if (queryName.Contains("Expensive"))
                    {
                        comm.CommandText = "INSERT INTO Schema.T_Expensive_Queries VALUES ";
                        foreach (DataRow row in dt.Rows)
                        {
                            comm.CommandText = comm.CommandText + "(NEWID(),CONVERT(uniqueidentifier,'" + serverID["FS_UID"] + "'),'" + row["session_id"] + "','" + row["HOST_NAME"] + "','" + row["PROGRAM_NAME"] + "','" + row["LOGIN_NAME"] + "','" + row["DB"] + "','" + row["STATUS"] + "','" + row["MEMORY_USAGE"] + "','" + row["CPU_TIME_ms"] + "','" + row["DURATION_ms"] + "','" + row["LOGICAL_READS"] + "','" + row["TEXT"] + "',CURRENT_TIMESTAMP),";
                        }
                        comm.CommandText = comm.CommandText.Remove(comm.CommandText.Length - 1);
                        comm.ExecuteNonQuery();
                    }

                    else if (queryName.Contains("Memory"))
                    {
                        comm.CommandText = "INSERT INTO Schema.T_CPU_Memory VALUES ";
                        foreach (DataRow row in dt.Rows)
                        {
                            comm.CommandText = comm.CommandText + "(NEWID(),CONVERT(uniqueidentifier,'" + serverID["FS_UID"] + "'),'" + row["cpu_usage"] + "','" + row["memory_usage"] + "',CURRENT_TIMESTAMP),";
                        }
                        comm.CommandText = comm.CommandText.Remove(comm.CommandText.Length - 1);
                        comm.ExecuteNonQuery();
                    }


                    //Closing the Connection to the database
                    oConnection.Close();
                }

            }

            //Call the method and return the recieved data
            return DataTable2String(dataTable);
        }

        //Method for Enhancing the generated Query Output Table
        public static string DataTable2String(DataTable dataTable)
        {
            //Initialize a StringBuilder which will keep our enhanced output
            StringBuilder sb = new StringBuilder();

            //If the Datable is null or there was no output
            if (dataTable != null)
            {
                //Separator between different columns
                string seperator = " | ";

                #region get min length for columns

                //Hashtable for storing the information regarding the space 
                Hashtable hash = new Hashtable();

                //Traverse through the columns and store its length in the hashTable
                foreach (DataColumn col in dataTable.Columns)
                    hash[col.ColumnName] = col.ColumnName.Length;

                //Traverse through the rows and store its length in the hashTable
                foreach (DataRow row in dataTable.Rows)
                    //Traverse a single row
                    for (int i = 0; i < row.ItemArray.Length; i++)
                        //If the row element isn't empty
                        if (row[i] != null)
                            //Check if its lenght is greater than the earlier stored length
                            if ((row[i].ToString().Replace("\t", "")).Length > (int)hash[dataTable.Columns[i].ColumnName])
                            {
                                //Assign it
                                hash[dataTable.Columns[i].ColumnName] = (row[i].ToString().Replace("\t", "")).Length;
                            }

               
                //To calculate the length vertically
                int rowLength = (hash.Values.Count + 1) * seperator.Length;
                
                foreach (object o in hash.Values)
                    rowLength += (int)o;

                
                #endregion get min length for columns

                // Making the design for the table
                sb.Append(new string('=', (rowLength - " Output ".Length) / 2));
                sb.Append(" Output ");
                sb.AppendLine(new string('=', (rowLength - " Output ".Length) / 2));
                
                //Writing the table Name
                if (!string.IsNullOrEmpty(dataTable.TableName))
                    sb.AppendLine(String.Format("{0,-" + rowLength + "}", String.Format("{0," + ((rowLength + dataTable.TableName.Length) / 2).ToString() + "}", dataTable.TableName)));

                #region write values
                //Writing the column Names
                foreach (DataColumn col in dataTable.Columns)
                    sb.Append(seperator + String.Format("{0,-" + hash[col.ColumnName] + "}", col.ColumnName));

                //Writing the separator
                sb.AppendLine(seperator);
                sb.AppendLine(new string('-', rowLength));
                
                //Writing the Data to the StringBuilder object
                foreach (DataRow row in dataTable.Rows)
                {
                    for (int i = 0; i < row.ItemArray.Length; i++)
                    {
                        sb.Append(seperator + (String.Format("{0," + hash[dataTable.Columns[i].ColumnName] + "}", row[i])).Replace("\t", ""));
                        if (i == row.ItemArray.Length - 1)
                            sb.AppendLine(seperator);
                    }
                }

                #endregion write values

                //Writing the End Design
                sb.AppendLine(new string('=', rowLength));
            }
            //If there was no output of the query
            else
                sb.AppendLine("================ Sorry , but there is no output for the specified query ================");

            //Return the Enhanced output as string
            return sb.ToString();
        }

        //Method used to fetch the data from the XML File
        public static void readXML(string logFileName, string filePath, string fileName, string serverName, string dbName)
        {
            //Catch If Any Exception occurs while Query Execution
            try
            {
                //Check If XML File and Log File Exists or Not
                if ((File.Exists(logFileName)) && (File.Exists(filePath + "\\" + fileName)))
                {
                    //Load the XML File in the Program
                    XDocument document = XDocument.Load(filePath + "\\" + fileName);

                    // Get the records of the XML file
                    var queryInfoCollection = from r in document.Descendants("QueryInfo")
                                              select new
                                              {
                                                  queryName = r.Element("QueryName").Value,//Store the 'QueryName'
                                                  query = r.Element("Query").Value,//Store the 'Query'
                                                  dbTable = r.Element("DBTable").Value,//Store the 'DBTable'
                                                  columnInformation = r.Element("ColumnInformation").Value,//Store the 'ColumnInformation'
                                                  activeStatus = r.Element("IsActive").Value//Store the 'IsActive' status
                                              };
                    
                    //A list to store the information
                    List<string> lines = new List<string>();

                    //Headers
                    lines.Add("                                                             Queries Information and Output ");
                    lines.Add(" ");
                    lines.Add(" ");

                    //Write it to the Log File
                    File.AppendAllLines(logFileName, lines.ToArray());
                    
                    //Clear the list for any earlier junk 
                    lines.Clear();




                    //Initialising a Sql Connection
                    SqlConnection Conn = new SqlConnection(@"Data Source=INPDDBA027\NGEP;Database=Dev_Server;Integrated Security=SSPI");
                    //Writing an SQL Query by passing in the connector and the query
                    SqlCommand comm = new SqlCommand("Select FS_UID,Server_Name FROM Schema.T_Server_Info", Conn);
                    Conn.Open();

                    serverIDList.Load(comm.ExecuteReader());

                    Conn.Close();


                    //Traverse the Rows of the information fetched from the xml file
                    foreach (var queryInfoRow in queryInfoCollection)
                    {
                        //Adding the Information to the List
                        lines.Add("Query Name         : " + queryInfoRow.queryName);
                        lines.Add("Query              : " + queryInfoRow.query.Trim().Replace("\t", ""));
                        lines.Add("DataBase Table     : " + queryInfoRow.dbTable);
                        lines.Add("Column Information : " + queryInfoRow.columnInformation);
                        lines.Add("Active Status      : " + queryInfoRow.activeStatus);
                        lines.Add(" ");
                        lines.Add("Query Output :-");
                        lines.Add(" ");

                        //Write it to the Log File
                        File.AppendAllLines(logFileName, lines.ToArray());
                        
                        //Write the Output of the query to the Log File
                        File.AppendAllText(logFileName, GetQueryOutput(queryInfoRow.query.Trim(), serverName, dbName,queryInfoRow.queryName.Trim()));
                        
                        //Write Some Gap to the Log File
                        File.AppendAllLines(logFileName, new string[] { "", "", "" });
                        
                        //Clear the list for any earlier junk 
                        lines.Clear();
                    }

                    serverIDList.Clear();
                }
                else
                {
                    //Throw exception if file is not found
                    throw new FileNotFoundException("File was not there at " + filePath);
                }

            }
            catch (Exception ex)
            {   //Catch If any exception got generated
                File.AppendAllText(logFileName, "Error : \n" + ex.ToString());
            }
        }

        //Method for writing the header Lines in the Log File
        public static void createLogFile(string filePath, string fileName, string logFileName, string serverName, string dbName)
        {
            // Write these Information to LogFile
            string[] topLines = {"                                                       LOG File          ",
                                "",
                                "",
                                "XML File name    : " + fileName,
                                "File Path        : " + filePath,
                                "Date of Creation : " + DateTime.Now.ToString("dd/MM/yyyy"),
                                "Time of Creation : " + DateTime.Now.ToString("HH:mm:ss"),
                                "Server Name      : " + serverName,
                                "Database Name    : " + dbName,
                                "",
                                "",
                                "",
                                "" 
                            };


            //Wrieing it to the log file
            File.WriteAllLines(logFileName, topLines);

        }
        
        
                /*
        public static void WriteOutput(string query)
        {
           string command = "bcp \""+query+"\" queryout " + Path.GetDirectoryName(logFileName) + "\\TempLog.txt -N -S " + serverName + " -c -T";
           System.Diagnostics.Process.Start("CMD.exe", command);

           //return System.Diagnostics.Process.Start("CMD.exe",command).StandardOutput.ReadToEnd();
           System.Diagnostics.Process proc = new System.Diagnostics.Process(); 
           proc.StartInfo.FileName = "cmd.exe"
           proc.StartInfo.Arguments = cmd; 
           proc.StartInfo.UseShellExecute = false;
           proc.StartInfo.RedirectStandardOutput = true; 
           proc.Start();

        }
        */

    }
}

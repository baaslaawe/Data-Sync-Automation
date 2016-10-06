using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Xml.Linq;
using System.Timers;
using System.IO;


/*
 * 
 * 
 * Author      : Prateek Sharma
 * Developer   : Prateek Sharma
 * Email-Id    : prateek13103718@gmail.com
 * Designation : Intern
 * 
 * 
 * 
 * */


/*
 * This is code for the service "Query Information Log File Maker".
 * In this , I have used no Library or anything.
 * I have used Costura.Fody from Nuget to generate the executable including all its depedencies so that it can run on any computer.
 * 
 * How to Install Nuget ???
 * 1. Open Visual Studio and go   Tools -> Extension Manager
 * 2. Click on Online Gallery (on the right side of screen)
 * 3. Seach Nuget and Install It
 * 
 * How to Install Costura.Fody ???
 * 1.  Open Visual Studio and go   Tools -> Nuget Package Manager->Package Manager Console
 * 2.  Then Type "Install-Package Costura.Fody"
 * 3.  Then Type "Install-CleanReferencesTarget"
 * Then from there,whenever you build or execute your program , an executable will be generated.
 * 
 * 
 * As for as the code , 
 *                  I have used a Timer which will schedule the service working. The Interval Time for the Service  will be calculated at the starting and at every Timer tick.
 *                  On starting the service , OnStart() method will be executed in which the starting time of the service will be recorded and nothing else.
 *                  After every timer tick (i.e. , when the timer has completedv it time interval ) , the method serviceTimerTickEventHandler() will be executed
 *                  (specified in the ElapsedTimeEventHandler on Line 152).
 *                  In that function, the configuration file will be used to fetch the information of database, server, and file path and name.
 *                  After that, methods like createLogFile(...) and readXML(...)  will be called.
 *                  These are static methods of the class 'LogDataServicing.cs' , so can be called by qualifying by its class name.
 *                  createLogFile() method will create the log file and write the necessary header information to it.
 *                  readXML() method will do the magic. It will fetch the information from the specified XML file and store it in the log file.
 *                  While doing that , it will call GetQueryOutput() method , another static method , to help it getting the output of the query specified in that particular query's information.
 *                  It will call DataTable2String() method to convert the fetch query output stored in the Data Table , into stro string and enhancing its looks a bit.
 *                  It will return the final string to GetQueryOutput() method,which inturn return to readXML() method.
 *                  readXML() method will store it in the file and move on.
 *                  On stopping the service , the report will be stored with the service stopping time and date.
 *                  One thing, whenever you are writing time , it should be in 24HR format.
 * Thank you.
 * If any doubts, feel free to not to mail me :P .
 * Just Kidding.
 * 
 * Best Regards,
 * Prateek Sharma.
 * 
 * */







namespace LogService
{
    public partial class Service1 : ServiceBase
    {
        // The Timer that will schedule the service
        private Timer serviceTimer = null;

        //The configurable's files , report and error log's file path and names
        private static string configurableFilesPath = "C:\\Users\\sha38475\\Desktop\\";
        private string Config_File = configurableFilesPath + "Config.ini";
        private string Report_LogFile = configurableFilesPath + "Report_Log.log";
        private string Error_LogFile = configurableFilesPath + "Error_Log.log";

        //For storing the Server Name fetched from the .ini file
        string serverName = "";
        //For storing the File Path of the .xml file which will be fetched from the .ini file
        string filePath = "";
        //For storing the File Name of the .xml file which will be fetched from the .ini file
        string fileName = "";
        //For storing the Database Name which will be fetched from the .ini file
        string dbName = "";


        //Constructor
        public Service1()
        {
            //To initialize the service and its components
            InitializeComponent();
        }

        //The triggered function when the service will be started
        protected override void OnStart(string[] args)
        {   
            // try-catch because we are handling File I/O
            try
            {
                // Checking if the Configuration file exists or not
                // It is by default kept at the Desktop
                if (File.Exists(Config_File))
                {

                    //Instantiate the Timer
                    serviceTimer = new Timer();

                    //For storing the list of time's fetched from the files
                    List<string> timeList = new List<string>();

                    // to get the data of the configuration files in the form of Lines 
                    string[] configData = File.ReadAllLines(Config_File);

                    //To recieve the key-value pairs
                    string[] pairs = new string[2];

                    //To traverse all lines in the .ini file
                    foreach (string line in configData)
                    {
                        //Split the lines 
                        pairs = line.Split('=').ToArray();
                        //if key=="Time" found , store it in the list
                        if(pairs[0].Trim() == "Time"){
                            timeList.Add(pairs[1].Trim());
                        }
                    }

                    //if no such time was found
                    if(timeList.Count() == 0){
                        //throw exception to stop the control flow  
                        throw new Exception("Proper Values for time not provided in the Config.ini file ......");
                        
                    }

                    //Sort the time list
                    timeList.Sort();

                    //if the time interval was initialized with a value
                    Boolean found = false;
                    
                    // Fetch today's date
                    DateTime today = DateTime.Now;
                    //Variable for date as per the given time in the .ini file
                    DateTime timeVariable;
                    //Loop through all time's
                    foreach(string time in timeList){
                        
                        //split the time's so as to get hours,mins,and secs
                        pairs = time.Split(':').ToArray();

                        //Conditions to check wheh=ther only hour was stated
                        if(pairs.Length == 1)
                            timeVariable = new DateTime(today.Year,today.Month,today.Day,Int32.Parse(pairs[0]),00,00);
                        //or hours and mins
                        else if(pairs.Length == 2)
                            timeVariable = new DateTime(today.Year,today.Month,today.Day,Int32.Parse(pairs[0]),Int32.Parse(pairs[1]),00);
                        //or hours,mins and secs
                        else
                            timeVariable = new DateTime(today.Year,today.Month,today.Day,Int32.Parse(pairs[0]),Int32.Parse(pairs[1]),Int32.Parse(pairs[2]));

                        //compare present time to the given time
                        if(today.TimeOfDay < timeVariable.TimeOfDay){
                            
                            //Calculate time difference in milliseconds from the found time
                            this.serviceTimer.Interval = timeVariable.Subtract(today).TotalMilliseconds;
                            //let know that the interval was given successfully
                            found = true;
                            //break off the loop
                            break;

                            //end of IF
                        }

                        //end of LOOP
                    }


                    //if interval was not given , the give it to the least one in the list as time for the next day
                    if(found == false){
                        
                        //Again same things
                        pairs = timeList[0].Split(':').ToArray();
                        if(pairs.Length == 1)
                            timeVariable = new DateTime(today.Year,today.Month,today.Day,Int32.Parse(pairs[0]),00,00);
                        else if(pairs.Length == 2)
                            timeVariable = new DateTime(today.Year,today.Month,today.Day,Int32.Parse(pairs[0]),Int32.Parse(pairs[1]),00);
                        else
                            timeVariable = new DateTime(today.Year,today.Month,today.Day,Int32.Parse(pairs[0]),Int32.Parse(pairs[1]),Int32.Parse(pairs[2]));

                        //Initialize for the next day's time
                        this.serviceTimer.Interval = timeVariable.AddDays(1).Subtract(today).TotalMilliseconds;
                        
                    }

                    //Assign the onTickListener of the service
                    //The method serviceTimerEventHandler will take care of everything
                    this.serviceTimer.Elapsed += new System.Timers.ElapsedEventHandler(this.serviceTimerTickEventHandler);
                    //Start the timer
                    this.serviceTimer.Enabled = true;

                    //Write the Report to report.log on the Desktop
                    File.AppendAllLines(Report_LogFile, new string[]{" "," ","Started on - " + DateTime.Now.ToString(),""});
                }
                // If the Configuration File is missing
                else
                {
                    // Throw the error
                    throw new FileNotFoundException("File wasn't found. Either the file isn't created or the directory path or file name is wrong !!!");
                }
            }
            catch (Exception ex)
            {
                // If any error caught , Write it in report.log
                File.AppendAllLines(Error_LogFile,new string[]{"","\nOccurance : "+ DateTime.Now.ToString()," Error : " + ex.ToString(),""});
                
            }
        }

        private void serviceTimerTickEventHandler(object sender, ElapsedEventArgs e)
        {
            //To cath any Unhandled Exception
            try
            {
                DateTime Initial = DateTime.Now;
                
                // to get the data of the configuration files in the form of Lines 
                string[] configData = File.ReadAllLines(Config_File);

                //For storing the list of time's fetched from the files
                List<string> timeList = new List<string>();

                //To recieve the key-value pairs
                string[] pairs = new string[2];

                //To traverse all lines in the .ini file
                foreach (string line in configData)
                {
                    //Split the lines
                    pairs = line.Split('=').ToArray();
                    
                    //Check for the key
                    switch (pairs[0].Trim())
                    {

                        case "Time": timeList.Add(pairs[1].Trim());
                            break;

                        case "Database_Name": dbName = pairs[1].Trim();
                            break;

                        case "XML_FileName": fileName = pairs[1].Trim();
                            break;

                        case "XML_FilePath": filePath = pairs[1].Trim();
                            break;

                        case "Server_Name": serverName = pairs[1].Trim();
                            break;

                    }
                     
                }

                //If their was no key - value pair for these
                if((dbName=="")||(serverName == "")||(fileName == "")||(filePath == "")){
                    
                    throw new Exception("Proper Configurable Key-Values not provided in the Config.ini file ......");
                
                }

                //if there is no XML file at the stated path
                if (!File.Exists(filePath + "\\" + fileName))
                {
                    throw new FileNotFoundException("The XML File stated in the .ini file was no found !!!! Please check if the directory or the file-Name is correct");
                }

                // Setting the generated Log File's Path
                string logFileName = filePath + "\\LogFile@" + DateTime.Now.ToString("dd-MM-yyyy_HHmmss") + ".log";


                // Calling the method for creating the file and writing the headers
                LogDataServicing.createLogFile(this.filePath, this.fileName, logFileName, this.serverName, this.dbName);

                // Method for Fetching data from XML File and getiing the output
                LogDataServicing.readXML(logFileName, this.filePath, this.fileName, this.serverName, this.dbName);


                if (timeList.Count() == 0)
                {

                    throw new Exception("Proper Values for time not provided in the Config.ini file ......");

                }

                //To check if the interval was given values or not
                Boolean found = false;
                // Fetch today's date
                DateTime today = DateTime.Now;
                //Variable for date as per the given time in the .ini file
                DateTime timeVariable;
                //Loop through all time's
                foreach (string time in timeList)
                {

                    //split the time's so as to get hours,mins,and secs
                    pairs = time.Split(':').ToArray();

                    //Conditions to check wheh=ther only hour was stated
                    if (pairs.Length == 1)
                        timeVariable = new DateTime(today.Year, today.Month, today.Day, Int32.Parse(pairs[0]), 00, 00);
                    //or hours and mins
                    else if (pairs.Length == 2)
                        timeVariable = new DateTime(today.Year, today.Month, today.Day, Int32.Parse(pairs[0]), Int32.Parse(pairs[1]), 00);
                    //or hours,mins and secs
                    else
                        timeVariable = new DateTime(today.Year, today.Month, today.Day, Int32.Parse(pairs[0]), Int32.Parse(pairs[1]), Int32.Parse(pairs[2]));

                    //compare present time to the given time
                    if (today.TimeOfDay < timeVariable.TimeOfDay)
                    {

                        //Calculate time difference in milliseconds from the found time
                        this.serviceTimer.Interval = timeVariable.Subtract(today).TotalMilliseconds;
                        //let know that the interval was given successfully
                        found = true;
                        //break off the loop
                        break;

                        //end of IF
                    }

                    //end of LOOP
                }


                //if interval was not given , the give it to the least one in the list as time for the next day
                if (found == false)
                {

                    //Again same things
                    pairs = timeList[0].Split(':').ToArray();
                    if (pairs.Length == 1)
                        timeVariable = new DateTime(today.Year, today.Month, today.Day, Int32.Parse(pairs[0]), 00, 00);
                    else if (pairs.Length == 2)
                        timeVariable = new DateTime(today.Year, today.Month, today.Day, Int32.Parse(pairs[0]), Int32.Parse(pairs[1]), 00);
                    else
                        timeVariable = new DateTime(today.Year, today.Month, today.Day, Int32.Parse(pairs[0]), Int32.Parse(pairs[1]), Int32.Parse(pairs[2]));

                    //Initialize for the next day's time
                    this.serviceTimer.Interval = timeVariable.AddDays(1).Subtract(today).TotalMilliseconds;

                }

                //to get the final time when our work is done
                DateTime final = DateTime.Now;

                //write it in the Report File
                File.AppendAllLines(Report_LogFile, new string[]{"","Log File : " + logFileName,"Service Processing time : " + (final.TimeOfDay - Initial.TimeOfDay).TotalMilliseconds.ToString() + " Milli-Seconds"});
            }
            //If any exception
            catch (Exception ex)
            {
                //Write it in the error_log
                File.AppendAllLines(Error_LogFile,new string[]{"","Error : ",ex.ToString()});
            }
        }

        protected override void OnStop()
        {
            //Stop the Timer
            serviceTimer.Enabled = false;

            //Write the info in report.log
            File.AppendAllLines(Report_LogFile,new string[]{"","Stopped at - " + DateTime.Now.ToString()});
        }
    }
}

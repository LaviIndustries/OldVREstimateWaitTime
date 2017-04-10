/// * -------------------------------------------------------------------------------------
/// <copyright  file="WTSimulatorService.Designer.cs" Company="Lavi Industries" Creater="Hai Wang"></Copyright>
///     Copyright (c) LaviIndustries (Hai Wang). All rights reserved.
/// </copyright>

using System.ServiceProcess;
using System.Timers;
using System.Data.SqlClient;
using System.Configuration;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Linq;

namespace WaitTimeSimulatorWindowsS
{
    public partial class WTSimulatorService : ServiceBase
    {
        private static System.Timers.Timer WTSimulatorTimer;
        private SqlConnection dbConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["WTSimDBConnection"].ConnectionString);
        private SqlCommand dbCommand = new SqlCommand();
        private IList<ActivatedBranch> threadpool = new List<ActivatedBranch>();


        public class ActivatedBranch
        {
            public string CompanyName { get; set; }
            public string Region { get; set; }
            public string Branch { get; set; }
            public string ID { get; set; }
        }

        public static List<ActivatedBranch> ActivatedBranchlistCache = new List<ActivatedBranch>();

        //public string CompanyName = null;
        //public string Region = null;
        //public string Branch = null;

        public WTSimulatorService()
        {
            InitializeComponent();

            if (!EventLog.SourceExists("WTSimulatorService"))
            {
                EventLog.CreateEventSource("WTSimulatorService", "WTSimulatorServiceLog");
            }
            eventLog1.Source = "WTSimulatorService";
            Debug.WriteLine("WTSimulatorService");

            InitializeBranchAST();

            WTSimulatorTimer = new System.Timers.Timer();
            WTSimulatorTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            WTSimulatorTimer.Interval = 5 * 1000;  // 5 secs
            WTSimulatorTimer.Enabled = true;
        }

        private bool InitializeBranchAST()
        {
            try
            {
                dbCommand.Connection = dbConnection;
                dbConnection.Open();
                eventLog1.WriteEntry("WTSimulator Windows Service Start...");
                Debug.WriteLine("WTSimulator Windows Service Start...");

                // Clear BranchSTAverageServiceTime Table
                string query = "TRUNCATE TABLE [BranchSTAverageServiceTime]";
                dbCommand.CommandText = query;
                dbCommand.ExecuteNonQuery();
                eventLog1.WriteEntry("Cleared BranchSTAverageServiceTime Table");
                Debug.WriteLine("Cleared BranchSTAverageServiceTime Table");

                // Initialize BranchSTAverageServiceTime Table filled with all Branched data
                query = "INSERT INTO [BranchSTAverageServiceTime] ([ServiceTypeName], [AverageServiceTime], [Branch], [Region], [Company]) SELECT x.[ServiceTypeName], AVG(DATEDIFF(SECOND, '19000101', y.[EventQueueTimeInsert] - x.[EventQueueTimeInsert])), x.[BranchName], x.[RegionName], x.[CompanyName] From (SELECT  [ServiceTypeName], [QueueId], [EventQueueTimeInsert], [BranchName], [RegionName], [CompanyName], [SessionId] From [Queue_Event_Shaddow] left Join [ServiceType] on [Queue_Event_Shaddow].QueueServiceType = [ServiceType].ServiceTypeId left Join [Branch] on [ServiceType].[ServiceTypeBranch] = [Branch].[BranchId] left join [Region] on [Branch].[BranchRegion] = [Region].[RegionId] left join [Company] on [Region].[RegionCompany] = [Company].[CompanyId] WHERE [EventId] = 7 and [ServiceTypeName] in (SELECT [ServiceTypeName] from [ServiceType])) x, (SELECT  [ServiceTypeName], [QueueId], [EventQueueTimeInsert], [BranchName], [RegionName], [CompanyName], [SessionId] From [Queue_Event_Shaddow] left Join [ServiceType] on[Queue_Event_Shaddow].QueueServiceType = [ServiceType].ServiceTypeId left Join [Branch] on [ServiceType].[ServiceTypeBranch] = [Branch].[BranchId] left join [Region] on [Branch].[BranchRegion] = [Region].[RegionId] left join [Company] on [Region].[RegionCompany] = [Company].[CompanyId] WHERE ([EventId] = 8 or [EventId] = 18) and [ServiceTypeName] in (SELECT [ServiceTypeName] from [ServiceType])) y WHERE x.[QueueId] = y.[QueueId] and x.EventQueueTimeInsert < y.EventQueueTimeInsert and DateDiff(dd, x.[EventQueueTimeInsert], getDate()) <= 30 and x.SessionId = y.SessionId GROUP BY x.[ServiceTypeName], x.[BranchName], x.[RegionName], x.[CompanyName] order by x.[BranchName]";
                dbCommand.CommandText = query;
                dbCommand.ExecuteNonQuery();
                eventLog1.WriteEntry("Initialized BranchSTAverageServiceTime Table");
                Debug.WriteLine("Initialized BranchSTAverageServiceTime Table");

                dbConnection.Close();

                return true;

            }
            catch (SqlException ex)
            {
                eventLog1.WriteEntry("*** Cannot read the data! ERRORs MESSAGE: " + ex.Message + " ***");
                Debug.WriteLine("*** Cannot read the data! ERRORs MESSAGE: " + ex.Message + " ***");

                return false;
            }
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("Starting WTSimulator Windows Service");
            Debug.WriteLine("Starting WTSimulator Windows Service");
            try
            {
                eventLog1.WriteEntry("OnTimed Updating Log");
                Debug.WriteLine("OnTimed Updating Log");
                eventLog1.WriteEntry("Starting WTS Timer");
                Debug.WriteLine("Starting WTS Timer");
                ThreadsM();
            }
            catch
            {
                eventLog1.WriteEntry("Cannot start WTS Timer", EventLogEntryType.Error);
                Debug.WriteLine("Cannot start WTS Timer", EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            try
            {
                eventLog1.WriteEntry("WTSimulator Windows Service Stop");
                Debug.WriteLine("WTSimulator Windows Service Stop");
            }
            catch
            {
                eventLog1.WriteEntry("Cannot Stop WTS Timer", EventLogEntryType.Error);
                Debug.WriteLine("Cannot Stop WTS Timer", EventLogEntryType.Error);
            }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                eventLog1.WriteEntry("OnTimed Updating log");
                Debug.WriteLine("OnTimed Updating log");
                ThreadsM();
                for (int i = 0; i < threadpool.Count; i++)
                {
                    Thread t1 = new Thread(new ParameterizedThreadStart(LogicProcess));
                    t1.IsBackground = true;
                    t1.Name = threadpool[i].CompanyName + threadpool[i].Region + threadpool[i].Branch;
                    t1.Start(threadpool[i]);
                    t1.Join();
                }
                Debug.WriteLine("Final End--------------------------------------------------------------------------");
                Debug.WriteLine("");
            }
            catch
            {
                eventLog1.WriteEntry("OnTimed Cannot Updating", EventLogEntryType.Error);
                Debug.WriteLine("OnTimed Cannot Updating", EventLogEntryType.Error);
            }
        }

        private bool ThreadsM()
        {
            bool returnValue = false;

            try
            {
                eventLog1.WriteEntry("Threads Management Start...");
                Debug.WriteLine("Thread Management Start...");

                dbCommand.Connection = dbConnection;
                dbConnection.Open();

                // Step 1 - Checking Activated Branch(es)
                List<ActivatedBranch> ActivatedBranchlist = new List<ActivatedBranch>();
                ActivatedBranchlist.Clear();

                string query = "SELECT CompanyName,RegionName,BranchName from [Branch] left join [Region] on [Branch].[BranchRegion] = [Region].[RegionId] left join [Company] on [Region].[RegionCompany] = [Company].[CompanyId] WHERE [BranchActive] = 1";
                dbCommand.CommandText = query;
                using (var reader = dbCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ActivatedBranch AB = new ActivatedBranch();
                        AB.CompanyName = reader.GetSqlValue(0).ToString();
                        AB.Region = reader.GetSqlValue(1).ToString();
                        AB.Branch = reader.GetSqlValue(2).ToString();
                        AB.ID = AB.CompanyName + AB.Region + AB.Branch;
                        ActivatedBranchlist.Add(AB);
                    }
                }
                dbConnection.Close();

                // Step 2 - Check current objects List with the cache list
                eventLog1.WriteEntry("### Activated Branch list Cache ###");
                Debug.WriteLine("### Activated Branch list Cache ###");
                for (int i = 0; i < ActivatedBranchlistCache.Count; i++)
                {
                    Debug.WriteLine("*** " + ActivatedBranchlistCache[i].CompanyName + " - " + ActivatedBranchlistCache[i].Region + " - " + ActivatedBranchlistCache[i].Branch + " ==> ActivatedBranchlistCache ***");
                }
                eventLog1.WriteEntry("### Activated Branch list ###");
                Debug.WriteLine("### Activated Branch list ###");
                for (int i = 0; i < ActivatedBranchlist.Count; i++)
                {
                    Debug.WriteLine("*** " + ActivatedBranchlist[i].CompanyName + " - " + ActivatedBranchlist[i].Region + " - " + ActivatedBranchlist[i].Branch + " ==> ActivatedBranchlist ***");
                }

                // -- Stop - Branch(es) in Cache not in Current
                //List<ActivatedBranch> ClosedBranchList = ActivatedBranchlistCache.Except(ActivatedBranchlist).ToList();
                List<ActivatedBranch> ClosedBranchList = new List<ActivatedBranch>();
                ClosedBranchList.AddRange(ActivatedBranchlistCache);
                for (int i = 0; i < ActivatedBranchlist.Count; i++)
                {
                    for (int j = 0; j < ClosedBranchList.Count; j++)
                    {
                        if (ActivatedBranchlist[i].ID == ClosedBranchList[j].ID)
                        {
                            ClosedBranchList.Remove(ClosedBranchList[j]);
                        }
                    }
                }
                // -- Start - Branch(es) in Current not in Cache
                //List<ActivatedBranch> NewStartedBranchList = ActivatedBranchlist.Except(ActivatedBranchlistCache).ToList();
                List<ActivatedBranch> NewStartedBranchList = new List<ActivatedBranch>();
                NewStartedBranchList.AddRange(ActivatedBranchlist);
                for (int i = 0; i < ActivatedBranchlistCache.Count; i++)
                {
                    for (int j = 0; j < NewStartedBranchList.Count; j++)
                    {
                        if (ActivatedBranchlistCache[i].ID == NewStartedBranchList[j].ID)
                        {
                            NewStartedBranchList.Remove(NewStartedBranchList[j]);
                        }
                    }
                }

                //ActivatedBranchlistCache = ActivatedBranchlistCache.Except(ClosedBranchList).ToList();
                for (int i = 0; i < ClosedBranchList.Count; i++)
                {
                    for (int j = 0; j < ActivatedBranchlistCache.Count; j++)
                    {
                        if (ClosedBranchList[i].ID == ActivatedBranchlistCache[j].ID)
                        {
                            ActivatedBranchlistCache.Remove(ActivatedBranchlistCache[j]);
                        }
                    }
                }
                ActivatedBranchlistCache.AddRange(NewStartedBranchList);

                // Step 3 - Close ClosedBranchList thread(s)
                eventLog1.WriteEntry("### Closed Branch List ###");
                Debug.WriteLine("### Closed Branch List ###");
                for (int i = 0; i < ClosedBranchList.Count; i++)
                {
                    eventLog1.WriteEntry("*** " + ClosedBranchList[i].CompanyName + " - " + ClosedBranchList[i].Region + " - " + ClosedBranchList[i].Branch + " ==> Stop ***");
                    Debug.WriteLine("*** " + ClosedBranchList[i].CompanyName + " - " + ClosedBranchList[i].Region + " - " + ClosedBranchList[i].Branch + " ==> Stop ***");
                    //WTSThreadPool pool = new WTSThreadPool(ClosedBranchList[i], true);
                    string id = ClosedBranchList[i].CompanyName + ClosedBranchList[i].Region + ClosedBranchList[i].Branch;
                    for (int j = 0; j < threadpool.Count; j++)
                    {
                        if (threadpool[i].ID == ClosedBranchList[j].ID)
                        {
                            threadpool.Remove(threadpool[j]);
                        }
                    }
                }

                // Step 4 - Start NewStartedBranchList thread(s)
                eventLog1.WriteEntry("### New Started Branch List ###");
                Debug.WriteLine("### New Started Branch List ###");
                for (int i = 0; i < NewStartedBranchList.Count; i++)
                {
                    eventLog1.WriteEntry("*** " + NewStartedBranchList[i].CompanyName + " - " + NewStartedBranchList[i].Region + " - " + NewStartedBranchList[i].Branch + " ==> Start ****");
                    Debug.WriteLine("*** " + NewStartedBranchList[i].CompanyName + " - " + NewStartedBranchList[i].Region + " - " + NewStartedBranchList[i].Branch + " ==> Start ****");
                    //WTSThreadPool pool = new WTSThreadPool(NewStartedBranchList[i], false);
                    threadpool.Add(NewStartedBranchList[i]);
                }

                eventLog1.WriteEntry("### Last Checking Activated Branch Cache List ###");
                Debug.WriteLine("### Last Checking Activated Branch Cache List ###");
                for (int i = 0; i < ActivatedBranchlistCache.Count; i++)
                {
                    eventLog1.WriteEntry("*** " + ActivatedBranchlistCache[i].CompanyName + " - " + ActivatedBranchlistCache[i].Region + " - " + ActivatedBranchlistCache[i].Branch + " ***");
                    Debug.WriteLine("*** " + ActivatedBranchlistCache[i].CompanyName + " - " + ActivatedBranchlistCache[i].Region + " - " + ActivatedBranchlistCache[i].Branch + " ***");
                }

            } catch (SqlException ex)
            {
                Debug.WriteLine("*** Thread ERRORs MESSAGE: " + ex.Message + " ***");
            }

            return returnValue;

        }

        private void LogicProcess(object obj)
        {
            List<string> listAva = new List<string>();
            List<string> listavserved = new List<string>();
            List<string> listavNoserved = new List<string>();

            try
            {
                if (obj is ActivatedBranch)
                {
                    ActivatedBranch operatedBranch = (ActivatedBranch)obj;
                    Debug.WriteLine("=== Thread - " + operatedBranch.CompanyName + " - " + operatedBranch.Region + " - " + operatedBranch.Branch + " ==> Doing ===");

                    // Main Logic
                    // Get Connection
                    dbCommand.Connection = dbConnection;
                    dbConnection.Open();

                    // Step 1 - Clear the table - WaitTimeList & WaitTimeListShaddow
                    string query = "TRUNCATE TABLE [WaitTimeListShaddow]";
                    dbCommand.CommandText = query;
                    dbCommand.ExecuteNonQuery();
                    dbCommand.CommandText = "TRUNCATE TABLE [WaitTimeList]";
                    dbCommand.ExecuteNonQuery();
                    //eventLog1.WriteEntry("Clear WTL and WTLS");

                    // Step 2 - Insert new data into the table - WaitTimeList
                    dbCommand.CommandText = "INSERT INTO [WaitTimeListShaddow] ([QueueId],[TicketId],[ServingUser],[ServiceTypeName],[EventQueueTimeInsert],[EventName],[Appointment],[Branch],[Region],[Company]) SELECT [QueueId],[Queue].[TicketId],[UserName],ServiceTypeName,[EventQueueTimeInsert],[EventName],[ScheduledTime],[BranchName],[RegionName],[CompanyName] From [Queue] Join [Queue_Event_Shaddow] on [Queue_Event_Shaddow].QueueId = [Queue].RowId Join [Queue_Shaddow] on [Queue_Event_Shaddow].QueueId = [Queue_Shaddow].RowId Join [Event] on [Queue_Event_Shaddow].EventId = [Event].EventId Join [SessionHistory] on [Queue_Event_Shaddow].SessionId = [SessionHistory].Id Join [User] on [SessionHistory].UserId = [User].UserId Join [ServiceType] on [Queue_Event_Shaddow].QueueServiceType = [ServiceType].ServiceTypeId Join [Branch] on [ServiceType].ServiceTypeBranch = [Branch].BranchId Join [Region] on [Branch].[BranchRegion] = [Region].[RegionId] Join [Company] on [Company].[CompanyId] = [Region].[RegionCompany] Where [CompanyName] = '" + operatedBranch.CompanyName + "' and [RegionName] = '" + operatedBranch.Region + "' and [BranchName] = '" + operatedBranch.Branch + "' and [BranchActive] = 1 and DateDiff(hh, EventQueueTimeInsert, getDate()) <= 6 order by [EventQueueTimeInsert] desc";
                    dbCommand.ExecuteNonQuery();
                    //eventLog1.WriteEntry("WTL insert Historical D successful");

                    // Step 3 - Delete those "Ended" records in the table - WaitTimeList
                    dbCommand.CommandText = "DELETE FROM [WaitTimeListShaddow] WHERE [QueueId] IN (SELECT [QueueId] FROM [WaitTimeList] WHERE [EventName] = 'End' or ([EventName] = ''))";
                    dbCommand.ExecuteNonQuery();
                    //eventLog1.WriteEntry("WTL delete End successful");

                    // Step 4 - Delete those NOISY records in the table - WaitTimeList
                    // 1. Delete those "Start" records in the table - WaitTimeList
                    dbCommand.CommandText = "DELETE FROM [WaitTimeListShaddow] WHERE [QueueId] IN (SELECT [QueueId] FROM [WaitTimeList] WHERE [EventName] = 'Start') and [EventName] = 'Issue ticket' or [EventName] = 'Send Next Sms' or  [EventName] = 'Send First Reminder Sms'";
                    dbCommand.ExecuteNonQuery();
                    //eventLog1.WriteEntry("WTL clear noisy events successful");

                    // get all the ticket(s) ID
                    dbCommand.CommandText = @"SELECT DISTINCT [TicketId] From [WaitTimeListShaddow]";
                    var listTicketID = new List<string>();
                    listTicketID.Clear();
                    using (var reader = dbCommand.ExecuteReader())
                    {
                        while (reader.Read())
                            listTicketID.Add(reader.GetSqlValue(0).ToString());
                    }

                    for (int j = 0; j < listTicketID.Count; j++)
                    {
                        dbCommand.CommandText = "INSERT INTO [WaitTimeList] ([QueueId],[TicketId],[ServingUser],[ServiceTypeName],[EventQueueTimeInsert],[EventName],[Appointment],[Branch],[Region],[Company]) SELECT TOP 1 [QueueId],[TicketId],[ServingUser],[ServiceTypeName],[EventQueueTimeInsert],[EventName],[Appointment],[Branch],[Region],[Company] FROM [WaitTimeListShaddow] where [TicketId] = '" + listTicketID[j] + "'";
                        dbCommand.ExecuteNonQuery();
                    }

                    dbCommand.CommandText = "DELETE FROM [WaitTimeList] WHERE [EventName] Not in ('Start', 'Issue ticket', 'Partial End', 'Transfer')";
                    dbCommand.ExecuteNonQuery();

                    // 2. Update the Ticket(s) Status
                    dbCommand.CommandText = "UPDATE [WaitTimeList] SET [Status] = ('In Queue') WHERE [EventName] = 'Issue ticket' or [EventName] = 'Partial End' or  [EventName] = 'Transfer'";
                    dbCommand.ExecuteNonQuery();

                    dbCommand.CommandText = "UPDATE [WaitTimeList] SET [Status] = ('In Service') WHERE [EventName] = 'Start'";
                    dbCommand.ExecuteNonQuery();

                    // 3. Update The Ticket(s)' assign user(s) Which "Issue Tickets"
                    dbCommand.CommandText = "UPDATE [WaitTimeList] SET [ServingUser] = ('') WHERE [EventName] = 'Issue ticket' or [EventName] = 'Partial End' or  [EventName] = 'Transfer'";
                    dbCommand.ExecuteNonQuery();

                    // Step 5 - Update and Assign the tickets in the wait list the stations 
                    dbCommand.CommandText = @"SELECT DISTINCT [ServingUser] From [WaitTimeList] WHERE [EventName] = 'Start'and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "'";
                    List<string> listStart = new List<string>();
                    using (var reader = dbCommand.ExecuteReader())
                    {
                        while (reader.Read())
                            listStart.Add(reader.GetSqlValue(0).ToString());
                    }

                    List<string> listAdditional = listavserved.Except(listStart).ToList();

                    listavserved.AddRange(listAdditional);
                    listAva.AddRange(listAdditional);

                    string listAvaList = "listAva: ";
                    for (int j = 0; j < listAva.Count; j++)
                    {
                        listAvaList = listAvaList + listAva[j] + "; ";
                    }
                    eventLog1.WriteEntry("Total Stations - " + listAva.Count() + " -- " + listAvaList);
                    Debug.WriteLine("Total Stations - " + listAva.Count() + " -- " + listAvaList + " -- " + operatedBranch.ID);

                    string listavservedList = "listavserved: ";
                    for (int j = 0; j < listavserved.Count; j++)
                    {
                        listavservedList = listavservedList + listavserved[j] + "; ";
                    }
                    eventLog1.WriteEntry("AVA Users Currently Serving - " + listavserved.Count() + " -- " + listavservedList);
                    Debug.WriteLine("AVA Users Currently Serving - " + listavserved.Count() + " -- " + listavservedList + " -- " + operatedBranch.ID);

                    string listavNoservedList = "listavNoserved: ";
                    for (int j = 0; j < listavNoserved.Count; j++)
                    {
                        listavNoservedList = listavNoservedList + listavNoserved[j] + "; ";
                    }
                    eventLog1.WriteEntry("AVA Users Not Currently Serving - " + listavNoserved.Count() + " -- " + listavNoservedList);
                    Debug.WriteLine("AVA Users Not Currently Serving - " + listavNoserved.Count() + " -- " + listavNoservedList + " -- " + operatedBranch.ID);

                    // 2. Check the waitlist
                    // On-Serve
                    dbCommand.CommandText = @"SELECT [ServingUser],[ServiceTypeName],[EstimateServiceTime] FROM [WaitTimeList] WHERE [EventName] = 'Start' and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "' ORDER BY [QueueId] desc";
                    int waitlistIndex1 = 0;
                    List<string> listWaitingTicketsS = new List<string>();

                    using (var reader = dbCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            waitlistIndex1++;
                            listWaitingTicketsS.Add(waitlistIndex1.ToString());
                            listWaitingTicketsS.Add(reader.GetSqlValue(0).ToString());
                            listWaitingTicketsS.Add(reader.GetSqlValue(1).ToString());
                            if (reader.GetSqlValue(2).ToString() == "Null")
                            {
                                listWaitingTicketsS.Add("0");
                            }
                            else
                            {
                                listWaitingTicketsS.Add(reader.GetSqlValue(2).ToString());
                            }
                        }
                    }

                    // On-Wait
                    int waitlistIndex2 = 0;
                    List<string> listWaitingTicketsW = new List<string>();

                    // Ticket(s) with Appointment
                    dbCommand.CommandText = @"SELECT [ServingUser],[ServiceTypeName],[EstimateWaitTime] From [WaitTimeList] WHERE (([EventName] = 'Issue ticket' and [Appointment] is not NULL) or ([EventName] = 'Partial End' and [Appointment] is not NULL) or ([EventName] = 'Transfer' and [Appointment] is not NULL)) and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "' ORDER BY [QueueId] desc";
                    using (var reader = dbCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            waitlistIndex2++;
                            listWaitingTicketsW.Add(waitlistIndex2.ToString());
                            listWaitingTicketsW.Add(reader.GetSqlValue(0).ToString());
                            listWaitingTicketsW.Add(reader.GetSqlValue(1).ToString());
                            if (reader.GetSqlValue(2).ToString() == "Null")
                            {
                                listWaitingTicketsW.Add("0");
                            }
                            else
                            {
                                listWaitingTicketsW.Add(reader.GetSqlValue(2).ToString());
                            }
                        }
                    }

                    // Ticket(s) without Appointment
                    dbCommand.CommandText = @"SELECT [ServingUser],[ServiceTypeName],[EstimateWaitTime] From [WaitTimeList] WHERE (([EventName] = 'Issue ticket' and [Appointment] is NULL) or ([EventName] = 'Partial End' and [Appointment] is NULL) or ([EventName] = 'Transfer' and [Appointment] is NULL)) and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "' ORDER BY [QueueId] desc";
                    using (var reader = dbCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            waitlistIndex2++;
                            listWaitingTicketsW.Add(waitlistIndex2.ToString());
                            listWaitingTicketsW.Add(reader.GetSqlValue(0).ToString());
                            listWaitingTicketsW.Add(reader.GetSqlValue(1).ToString());
                            if (reader.GetSqlValue(2).ToString() == "Null")
                            {
                                listWaitingTicketsW.Add("0");
                            }
                            else
                            {
                                listWaitingTicketsW.Add(reader.GetSqlValue(2).ToString());
                            }
                        }
                    }

                    // update On-serve EstimateWaitTime
                    for (int j = 0; j < listWaitingTicketsS.Count / 4; j++)
                    {
                        string stationName = listWaitingTicketsS[j * 4 + 1];
                        string ServiceType = listWaitingTicketsS[j * 4 + 2];

                        dbCommand.CommandText = "UPDATE [WaitTimeList] SET [EstimateServiceTime] = (SELECT [AverageServiceTime] FROM [BranchSTAverageServiceTime] WHERE [ServiceTypeName] = '" + ServiceType + "') WHERE [ServingUser] = '" + stationName + "' and [ServiceTypeName] = '" + ServiceType + "' and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "'";
                        dbCommand.ExecuteNonQuery();
                        //sqlCommand.CommandText = "UPDATE [WaitTimeList] SET [EstimateServiceTime] = (SELECT AVG([AverageServiceTime]) FROM [AverageStorage2] WHERE [UserName]= '" + stationName + "' and [ServiceTypeName] = '" + ServiceType + "' ) WHERE [ServingUser] = '" + stationName + "' and [ServiceTypeName] = '" + ServiceType + "'";
                        //sqlCommand.ExecuteNonQuery();
                        eventLog1.WriteEntry("Update Estimate Service successful");
                        Debug.WriteLine("Update Estimate Service successful" + " -- " + operatedBranch.ID);

                    }

                    // Available Station(s) Accumulate Time
                    // Set up Initial Accumulate Time - NoServed = 0 or Served = EST
                    List<string> listNoServicedStationAccumulateTime = new List<string>();
                    List<string> listServicedStationAccumulateTime = new List<string>();
                    for (int j = 0; j < listavNoserved.Count; j++)
                    {
                        listNoServicedStationAccumulateTime.Add(listavNoserved[j]);
                        listNoServicedStationAccumulateTime.Add("0");
                    }
                    for (int j = 0; j < listavserved.Count; j++)
                    {
                        listServicedStationAccumulateTime.Add(listavserved[j]);
                        string EstimateServiceTime = null;
                        dbCommand.CommandText = @"SELECT [EstimateServiceTime] From [WaitTimeList] WHERE [EventName] = 'Start' and [ServingUser] = '" + listavserved[j] + "' and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "'";
                        using (var reader = dbCommand.ExecuteReader())
                        {
                            while (reader.Read())
                                EstimateServiceTime = reader.GetSqlValue(0).ToString();
                        }
                        listServicedStationAccumulateTime.Add(EstimateServiceTime);
                    }

                    // Step 6 - Update Accumulate Time and WTL WaitTime
                    // 1. Clear the table
                    dbCommand.CommandText = "TRUNCATE TABLE [AccumulateTimeStorage]";
                    dbCommand.ExecuteNonQuery();

                    // 2. Insert Initial Accumulate Time for each station each service type - OnServed and NoServed
                    // (1) NoServed
                    for (int j = 0; j < listNoServicedStationAccumulateTime.Count / 2; j++)
                    {
                        dbCommand.CommandText = @"SELECT [ServiceTypeName] FROM [BranchUserItem] Join [User] on [BranchUserItem].[UserId] = [User].[UserId] Join [ServiceType] on [BranchUserItem].[EntityId] = [ServiceType].ServiceTypeId WHERE [UserName] = '" + listNoServicedStationAccumulateTime[j * 2] + "' and [EntityType] = 'service_type'";
                        List<string> listAddST1 = new List<string>();

                        using (var reader = dbCommand.ExecuteReader())
                        {
                            while (reader.Read())
                                listAddST1.Add(reader.GetSqlValue(0).ToString());
                        }

                        for (int i = 0; i < listAddST1.Count; i++)
                        {
                            dbCommand.CommandText = "INSERT INTO [AccumulateTimeStorage] ([UserName],[ServiceType],[AccumulateTime],[Branch],[Region],[Company]) VALUES ('" + listNoServicedStationAccumulateTime[j * 2] + "', '" + listAddST1[i] + "', '" + listNoServicedStationAccumulateTime[j * 2 + 1] + "', '" + operatedBranch.Branch + "', '" + operatedBranch.Region + "', '" + operatedBranch.CompanyName + "')";
                            dbCommand.ExecuteNonQuery();
                            eventLog1.WriteEntry("NoServed Initial Accumulate Time OnService successful");
                            Debug.WriteLine("NoServed Initial Accumulate Time OnService successful" + " -- " + operatedBranch.ID);
                        }
                    }

                    // (2) OnServed
                    for (int j = 0; j < listServicedStationAccumulateTime.Count / 2; j++)
                    {
                        dbCommand.CommandText = @"SELECT [ServiceTypeName] FROM [BranchUserItem] Join [User] on [BranchUserItem].[UserId] = [User].[UserId] Join [ServiceType] on [BranchUserItem].[EntityId] = [ServiceType].ServiceTypeId WHERE [UserName] = '" + listServicedStationAccumulateTime[j * 2] + "' and [EntityType] = 'service_type'";
                        var listAddST2 = new List<string>();

                        using (var reader = dbCommand.ExecuteReader())
                        {
                            while (reader.Read())
                                listAddST2.Add(reader.GetSqlValue(0).ToString());
                        }


                        for (int i = 0; i < listAddST2.Count; i++)
                        {
                            dbCommand.CommandText = "INSERT INTO [AccumulateTimeStorage] ([UserName],[ServiceType],[AccumulateTime],[Branch],[Region],[Company]) VALUES ('" + listNoServicedStationAccumulateTime[j * 2] + "', '" + listAddST2[i] + "', '" + listNoServicedStationAccumulateTime[j * 2 + 1] + "', '" + operatedBranch.Branch + "', '" + operatedBranch.Region + "', '" + operatedBranch.CompanyName + "')";
                            dbCommand.ExecuteNonQuery();
                            eventLog1.WriteEntry("Served Initial Accumulate Time OnService successful");
                            Debug.WriteLine("Served Initial Accumulate Time OnService successful" + " -- " + operatedBranch.ID);
                        }
                    }

                    // (3) Update the Waiting tickets(Issue Tickets), assign station
                    // (3.1) List of Issue ticket event in the wait list
                    // Ticket(s) with(out) Appointment
                    List<string> listCheckIssueTicket = new List<string>();
                    List<string> OnCallAppointmentTicket = new List<string>();

                    dbCommand.CommandText = @"SELECT [QueueId],[ServiceTypeName],[Appointment] From [WaitTimeList] WHERE ([EventName] = 'Issue ticket' or [EventName] = 'Partial End' or [EventName] = 'Transfer') and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "' ORDER BY [EventQueueTimeInsert] desc";
                    using (var reader = dbCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            //Debug.WriteLine(reader.GetSqlValue(2).ToString());
                            if (reader.GetSqlValue(2).ToString() == "Null") // Without Appointment
                            {
                                //Debug.WriteLine("Not Appointment");
                                listCheckIssueTicket.Add(reader.GetSqlValue(0).ToString());
                                listCheckIssueTicket.Add(reader.GetSqlValue(1).ToString());
                            }
                            if (reader.GetSqlValue(2).ToString() != "Null") // With Appointment
                            {

                                int result = DateTime.Compare(Convert.ToDateTime(reader.GetSqlValue(2).ToString()), DateTime.Now);
                                //Debug.WriteLine(result);

                                if (result <= 0) // Schedular matched, assign to a new oncall list
                                {
                                    //Debug.WriteLine("Appointment call");
                                    OnCallAppointmentTicket.Add(reader.GetSqlValue(0).ToString());
                                    OnCallAppointmentTicket.Add(reader.GetSqlValue(1).ToString());
                                }
                                else // Schedular not matched
                                {
                                    //Debug.WriteLine("Appointment not call yet");
                                    listCheckIssueTicket.Add(reader.GetSqlValue(0).ToString());
                                    listCheckIssueTicket.Add(reader.GetSqlValue(1).ToString());
                                }
                            }
                        }
                    }

                    // (3.2) assign OnCallAppointmentTicket list ticket(s) on the top of the Waiting list
                    if (OnCallAppointmentTicket.Count > 0)
                    {
                        //Debug.WriteLine("Got it");
                        for (int j = 0; j < OnCallAppointmentTicket.Count; j++)
                        {
                            listCheckIssueTicket.Add(OnCallAppointmentTicket[j]);
                            //Debug.WriteLine(OnCallAppointmentTicket[j]);
                        }
                    }

                    // (3.3) Assign the ticket(s) on the Waiting list a station
                    for (int j = 0; j < listCheckIssueTicket.Count / 2; j++)
                    {
                        eventLog1.WriteEntry("*************************************");
                        Debug.WriteLine("*************************************" + " -- " + operatedBranch.ID);

                        eventLog1.WriteEntry("Ticket ID: " + listCheckIssueTicket[j * 2] + "; Service Type: " + listCheckIssueTicket[j * 2 + 1]);
                        Debug.WriteLine("Ticket ID: " + listCheckIssueTicket[j * 2] + "; Service Type: " + listCheckIssueTicket[j * 2 + 1] + " -- " + operatedBranch.ID);

                        string AssignStation = null;

                        // Find the shortest and has ST Station
                        dbCommand.CommandText = @"SELECT [UserName] FROM [AccumulateTimeStorage] WHERE [ServiceType] = '" + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2 + 1] + "' and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "' ORDER BY [AccumulateTime] asc";
                        object r = dbCommand.ExecuteScalar();
                        if (r == null)
                        {
                            eventLog1.WriteEntry("No avali station, do nothing!");
                            Debug.WriteLine("No avali station, do nothing!" + " -- " + operatedBranch.ID);
                        }
                        else
                        {
                            AssignStation = r.ToString();
                            eventLog1.WriteEntry("Assign station: " + AssignStation);
                            Debug.WriteLine("Assign station: " + AssignStation + " -- " + operatedBranch.ID);

                            // Update the AccumulateTime
                            string OldAccumulateTime = null;
                            //string NewAssignStation = null;
                            //sqlCommand.CommandText = @"SELECT AVG([AverageServiceTime]), [UserName] FROM [AverageStorage2] WHERE [UserName] = '" + AssignStation + "' and [ServiceTypeName] = '" + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2 + 1] + "' GROUP BY [UserName]";
                            dbCommand.CommandText = @"SELECT [AverageServiceTime] FROM [BranchSTAverageServiceTime] WHERE [ServiceTypeName] = '" + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2 + 1] + "' and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "'";
                            using (var reader = dbCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    OldAccumulateTime = reader.GetSqlValue(0).ToString();
                                    //NewAssignStation = reader.GetSqlValue(1).ToString();
                                }
                            }
                            eventLog1.WriteEntry("Assign station Median Service Time: " + OldAccumulateTime);
                            Debug.WriteLine("Assign station Median Service Time: " + OldAccumulateTime + " -- " + operatedBranch.ID);

                            dbCommand.CommandText = @"SELECT [AccumulateTime] FROM [AccumulateTimeStorage] WHERE [UserName] = '" + AssignStation + "' and [ServiceType] = '" + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2 + 1] + "' and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "'";
                            string CurrentAccumulateTime = dbCommand.ExecuteScalar().ToString();
                            eventLog1.WriteEntry("Assign station Current Accumulate Time: " + CurrentAccumulateTime);
                            Debug.WriteLine("Assign station Current Accumulate Time: " + CurrentAccumulateTime + " -- " + operatedBranch.ID);

                            string NewAccumulateTime = null;
                            NewAccumulateTime = Convert.ToString(Convert.ToInt64(OldAccumulateTime) + Convert.ToInt64(CurrentAccumulateTime));
                            eventLog1.WriteEntry("New Accumulate Time: " + NewAccumulateTime);
                            Debug.WriteLine("New Accumulate Time: " + NewAccumulateTime + " -- " + operatedBranch.ID);

                            dbCommand.CommandText = "UPDATE [AccumulateTimeStorage] SET [AccumulateTime] = " + NewAccumulateTime + " WHERE [UserName] = '" + AssignStation + "' and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "'";
                            dbCommand.ExecuteNonQuery();
                            eventLog1.WriteEntry("Update New Accumulate Time successful" + "-----------" + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2] + ", " + NewAccumulateTime + ", " + AssignStation);
                            Debug.WriteLine("Update New Accumulate Time successful" + "-----------" + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2] + ", " + NewAccumulateTime + ", " + AssignStation + " -- " + operatedBranch.ID);
                            //Debug.Write(", " + OldAccumulateTime);
                            //Debug.WriteLine(", " + NewAssignedStationName);

                            dbCommand.CommandText = "UPDATE [WaitTimeList] SET [EstimateWaitTime] = " + CurrentAccumulateTime + " ,[ServingUser] = '" + AssignStation + "' WHERE [QueueId] = " + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2] + " and [ServiceTypeName] = '" + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2 + 1] + "' and [Branch] = '" + operatedBranch.Branch + "' and [Region] = '" + operatedBranch.Region + "' and [Company] = '" + operatedBranch.CompanyName + "'";
                            dbCommand.ExecuteNonQuery();
                            eventLog1.WriteEntry("Update WL Estimate WaitTime successful");
                            Debug.WriteLine("Update WL Estimate WaitTime successful" + " -- " + operatedBranch.ID);

                            // Store the first estimate waittime in table - HistoricalEstimateTimeStorage
                            dbCommand.CommandText = @"SELECT TOP 300 [RowId] FROM [HistoricalEstimateTimeStorage] order by [RowId] DESC";
                            List<string> TicketsName = new List<string>();
                            using (var reader = dbCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    TicketsName.Add(reader.GetSqlValue(0).ToString());
                                }
                            }

                            dbCommand.CommandText = "INSERT INTO [HistoricalEstimateTimeStorage-AllEstimateTCollection] ([RowId],[TimeStemp],[ServingUser],[ServiceTypeName],[EstimateWaitTime],[Branch],[Region],[Company]) VALUES ('" + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2] + "',GETDATE(),'" + AssignStation + "','" + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2 + 1] + "','" + CurrentAccumulateTime + "','" + operatedBranch.Branch + "','" + operatedBranch.Region + "','" + operatedBranch.CompanyName + "')";
                            dbCommand.ExecuteNonQuery();
                            eventLog1.WriteEntry("INSERT in HistoricalEstimateTimeStorage-AllEstimateTCollection - contained");
                            Debug.WriteLine("INSERT in HistoricalEstimateTimeStorage-AllEstimateTCollection - contained" + " -- " + operatedBranch.ID);

                            if (TicketsName.Contains(listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2]))
                            {
                                eventLog1.WriteEntry("Is contains!!!!");
                                Debug.WriteLine("Is contains!!!!" + " -- " + operatedBranch.ID);
                                //Debug.WriteLine("Is contains!!!!");
                            }
                            else
                            {
                                dbCommand.CommandText = "INSERT INTO [HistoricalEstimateTimeStorage] ([RowId],[ServingUser],[ServiceTypeName],[EstimateWaitTime],[Branch],[Region],[Company]) VALUES ('" + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2] + "','" + AssignStation + "','" + listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2 + 1] + "','" + CurrentAccumulateTime + "','" + operatedBranch.Branch + "','" + operatedBranch.Region + "','" + operatedBranch.CompanyName + "')";
                                dbCommand.ExecuteNonQuery();
                                eventLog1.WriteEntry(listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2] + "; " + CurrentAccumulateTime + " - Insert table 'HistoricalEstimateTimeStorage' successful!");
                                Debug.WriteLine(listCheckIssueTicket[(listCheckIssueTicket.Count / 2 - 1 - j) * 2] + "; " + CurrentAccumulateTime + " - Insert table 'HistoricalEstimateTimeStorage' successful!" + " -- " + operatedBranch.ID);
                            }
                        }
                    }

                    dbConnection.Close();

                }

            } catch (SqlException ex)
            {
                eventLog1.WriteEntry("*** Process ERRORs MESSAGE: " + ex.Message + " ***");
                Debug.WriteLine("*** Process ERRORs MESSAGE: " + ex.Message + " ***");
            }
            
        }

        private void eventLog1_EntryWritten(object sender, EntryWrittenEventArgs e)
        {
        }
    }
}

/// <copyright  file="WTSimulatorService.Designer.cs" Company="Lavi Industries" Creater="Hai Wang"></Copyright>
///     Copyright (c) LaviIndustries (Hai Wang). All rights reserved.
/// </copyright>
/// * -------------------------------------------------------------------------------------
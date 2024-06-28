using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml;

class Program
{
    static Dictionary<string, (string name, string location)> storeMapping = new Dictionary<string, (string name, string location)>
    {
        { "storeID", ("Store 1", "Store 1 Location") },
        { "0081", ("Store 1", "Store 1 Location") },
        { "0060", ("Store 2", "Store 2 Location") },
        { "0030", ("Store 3", "Store 3 Location") },
        { "0012", ("Store 4", "Store 4 Location") },
        { "0007", ("Store 5", "Store 5 Location") },
        { "0040", ("Store 6", "Store 6 Location") },
        { "0009", ("Store 7", "Store 7 Location") }
    };

    static async Task Main(string[] args)
    {
        string connectionString = "YOUR_CONNECTION_STRING_HERE";
        string apiUrlTemplate = "https://stws.shoppertrak.com/EnterpriseFlash/v1.0/service/allsites?start_time={0}&end_time={1}";
        string username = "YOUR_API_USERNAME";
        string password = "YOUR_API_PASSWORD";

        try
        {
            string apiUrl = GenerateApiUrl(apiUrlTemplate, null);  // Passing null to use current time
            Console.WriteLine($"Generated API URL: {apiUrl}");

            string xmlData = await GetApiData(apiUrl, username, password);
            Console.WriteLine("Data fetched from API.");

            if (ValidateTableStructure(connectionString))
            {
                Console.WriteLine("Table structure validated.");
                InsertDataIntoDatabase(xmlData, connectionString);
            }
            else
            {
                Console.WriteLine("Table structure validation failed. Please check your database schema.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Stack Trace:");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static string GenerateApiUrl(string apiUrlTemplate, DateTime? customEndTime)
    {
        DateTime localDate = DateTime.Now.Date;  // Today's date at midnight
        TimeSpan startTime = new TimeSpan(10, 0, 0);  // 10:00 AM

        DateTime localStartDateTime = DateTime.SpecifyKind(localDate + startTime, DateTimeKind.Local);

        // Determine the end time
        DateTime localEndDateTime;
        if (customEndTime.HasValue)
        {
            localEndDateTime = customEndTime.Value;
        }
        else
        {
            localEndDateTime = DateTime.Now;
            // Round to the nearest 15-minute increment
            int minutes = localEndDateTime.Minute;
            int remainder = minutes % 15;
            localEndDateTime = localEndDateTime.AddMinutes(-remainder);
        }

        DateTime utcStartDateTime = TimeZoneInfo.ConvertTimeToUtc(localStartDateTime);
        DateTime utcEndDateTime = TimeZoneInfo.ConvertTimeToUtc(localEndDateTime);

        string startTimeStr = utcStartDateTime.ToString("yyyyMMddHHmm");
        string endTimeStr = utcEndDateTime.ToString("yyyyMMddHHmm");

        return string.Format(apiUrlTemplate, startTimeStr, endTimeStr);
    }

    static async Task<string> GetApiData(string apiUrl, string username, string password)
    {
        using (HttpClient client = new HttpClient())
        {
            var byteArray = new System.Text.ASCIIEncoding().GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            HttpResponseMessage response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }

    static bool ValidateTableStructure(string connectionString)
    {
        string query = "SELECT column_name FROM all_tab_columns WHERE table_name = 'STORETRAFFIC' AND (column_name = 'SITEID' OR column_name = 'TRAFFICDATETIME' OR column_name = 'TRAFFICIN' OR column_name = 'TRAFFICOUT' OR column_name = 'TRAFFICTIME' OR column_name = 'NAME')";

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            using (OleDbCommand cmd = new OleDbCommand(query, conn))
            {
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    int columnCount = 0;
                    while (reader.Read())
                    {
                        columnCount++;
                    }
                    return columnCount == 6;
                }
            }
        }
    }

    static void InsertDataIntoDatabase(string xmlData, string connectionString)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xmlData);

        XmlNodeList siteNodes = doc.SelectNodes("//site");

        if (siteNodes == null || siteNodes.Count == 0)
        {
            Console.WriteLine("No site nodes found in the XML data.");
            return;
        }

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            foreach (XmlNode siteNode in siteNodes)
            {
                if (siteNode.Attributes["storeID"] == null)
                {
                    Console.WriteLine("storeID attribute not found.");
                    continue;
                }

                string storeId = siteNode.Attributes["storeID"].Value; // Mapping storeID to SITEID
                if (!storeMapping.ContainsKey(storeId))
                {
                    Console.WriteLine($"StoreID {storeId} not found in store mapping.");
                    continue;
                }

                string name = storeMapping[storeId].name;
                XmlNodeList trafficNodes = siteNode.SelectNodes("traffic");

                if (trafficNodes == null || trafficNodes.Count == 0)
                {
                    Console.WriteLine($"No traffic nodes found for storeID {storeId}.");
                    continue;
                }

                foreach (XmlNode trafficNode in trafficNodes)
                {
                    if (trafficNode.Attributes["enters"] == null || trafficNode.Attributes["exits"] == null || trafficNode.Attributes["startTime"] == null)
                    {
                        Console.WriteLine($"Incomplete traffic data for storeID {storeId}.");
                        continue;
                    }

                    int trafficIn = int.Parse(trafficNode.Attributes["enters"].Value);
                    int trafficOut = int.Parse(trafficNode.Attributes["exits"].Value);
                    string startTime = trafficNode.Attributes["startTime"].Value;
                    DateTime trafficDateTime;

                    if (!DateTime.TryParseExact(startTime, "yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out trafficDateTime))
                    {
                        Console.WriteLine($"Invalid startTime format for storeID {storeId}: {startTime}");
                        continue;
                    }

                    // Extracting the time component as a string
                    string trafficTime = trafficDateTime.ToString("HH:mm:ss");

                    string query = "INSERT INTO STORETRAFFIC (SITEID, TRAFFICDATETIME, TRAFFICIN, TRAFFICOUT, TRAFFICTIME, NAME) VALUES (?, ?, ?, ?, ?, ?)";

                    using (OleDbCommand cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SiteId", storeId);
                        cmd.Parameters.AddWithValue("@TrafficDateTime", trafficDateTime);
                        cmd.Parameters.AddWithValue("@TrafficIn", trafficIn);
                        cmd.Parameters.AddWithValue("@TrafficOut", trafficOut);
                        cmd.Parameters.AddWithValue("@TrafficTime", trafficTime);
                        cmd.Parameters.AddWithValue("@Name", name);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        Console.WriteLine("Data inserted into the database successfully.");
    }
}

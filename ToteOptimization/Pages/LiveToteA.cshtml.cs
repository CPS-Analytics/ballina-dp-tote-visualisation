using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ToteOptimization.Models.Chart;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
namespace ToteOptimization.Pages
{
    public class LiveToteAModel : PageModel
    {
        public ChartJs Chart2 { get; set; }
        public ChartJs Chart3 { get; set; }
        public ChartJs Chart4 { get; set; }
        public ChartJs Chart5 { get; set; }
        public string ChartJson2 { get; set; }
        public string ChartJson3 { get; set; }
        public string ChartJson4 { get; set; }
        public string ChartJson5 { get; set; }
        public int Current_PO_Bags { get; set; }

        public string PODetails { get; set; }
        public string PO { get; set; }

        private readonly ILogger<LiveToteAModel> _logger;
        private readonly IConfiguration _configuration;

        public LiveToteAModel(ILogger<LiveToteAModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        public ChartJs chartPop(string index, Dictionary<string, List<string>> Heads)
        {
            var chartData = @"
        {
            type: 'bar',
            responsive: false,
            maintainAspectRatio: false,
            drops: 0,
            secsToEmpty: 'No Tote',
            totes: 0,
            data:
            {
                labels: ['Tote_Lifted'],
                datasets: [{
                    label: 'Kg of Material',
                    data: [0],
                    backgroundColor: ['white'],
                }]
            },
            options:
            {
                legend:
                {
                    display: false
                },
                scales:
                {
                    yAxes: [{
                        ticks:
                        {
                            beginAtZero: true,
                            suggestedMax: 1000
                        }
                    }],
                    xAxes: [{
                        display: false
                    }]
                },
            }
        }";
            var chart = JsonConvert.DeserializeObject<ChartJs>(chartData);
            if (Heads.ContainsKey(index))
            {
                List<string> head = Heads[index];
                chart.data.datasets[0].data[0] = Int32.Parse(head[4]);
                chart.data.datasets[0].backgroundColor[0] = head[6];
                chart.data.labels[0] = "Tote " + head[0] + "_" + head[1];
                chart.drops = head[5];
                chart.secsToEmpty = head[7];
                if (head[9] != "Not In Current PO") chart.totes = head[10];
            }
            return chart;
        }
        public void OnGet()
        {

            PO = "Idle No PO";
            List<string> headData = new List<string>();
            Dictionary<string, List<string>> Heads = new Dictionary<string, List<string>>();

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.ConnectionString = _configuration.GetConnectionString("B61");
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    string spName = @"dbo.[sp_4A_GetToteInfo]";
                    using (SqlCommand command = new SqlCommand(spName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 60;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                headData.Add(reader.GetInt32(1).ToString()); //tote             0
                                headData.Add(reader.GetInt32(2).ToString()); //material         1
                                headData.Add(reader.GetDouble(3).ToString()); //target         2
                                headData.Add(reader.GetDateTime(4).ToString()); //last drop     3
                                headData.Add(reader.GetInt32(5).ToString()); //quantity         4

                                if (reader[10] != DBNull.Value)
                                {
                                    PO = reader.GetString(10);
                                    headData.Add(reader.GetInt32(6).ToString()); //drops            5
                                    if (reader.GetInt32(6) > 200) headData.Add("green");//colours   6
                                    else if (reader.GetInt32(6) > 75) headData.Add("orange");
                                    else headData.Add("red");
                                    //headData.Add(DateTime.Now.AddSeconds(reader.GetInt32(7)).ToString("h:mm tt")); //Time Left 7 TimeSpan.FromSeconds(reader.GetInt32(8)).ToString()
									headData.Add(TimeSpan.FromSeconds(reader.GetInt32(7)).ToString(@"hh\:mm\:ss"));
                                    headData.Add(reader.GetInt32(9).ToString()); //Total_Bags_Dropped 8
                                    headData.Add(reader.GetString(10)); //PO 9
                                }
                                else
                                {
                                    headData.Add("0");
                                    headData.Add("grey");
                                    headData.Add("Not In Use");
                                    headData.Add("0");
                                    headData.Add("Not In Current PO");
                                }
                                Heads.Add(reader.GetString(0), new List<string>(headData));
                                headData.Clear();
                            }
                            reader.Close();
                        }
                    }
                    connection.Close();
                }

            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            if (PO != "Idle No PO")
            {
                try
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                    builder.ConnectionString = _configuration.GetConnectionString("B106");
                    using (SqlConnection connection106 = new SqlConnection(builder.ConnectionString))
                    {
                        string spName = @"dbo.[spDPGetBagQuantityOrdered]";
                        connection106.Open();
                        using (SqlCommand command = new SqlCommand(spName, connection106))
                        {
                            command.Parameters.AddWithValue("@PO", PO);
                            command.CommandType = CommandType.StoredProcedure;
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Current_PO_Bags = reader.GetInt32(1);
                                }
                                reader.Close();
                            }
                        }
                        connection106.Close();
                    }
                }
                catch (SqlException e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            else Current_PO_Bags = 0;
            int Total_Kg_Rem;
            decimal temp;
            int Totes_Rem;
            List<string> head;
            int i;
            string headName;
            for (i = 0; i < Heads.Count; i++)
            {
                head = Heads.Values.ElementAt(i);
                headName = Heads.Keys.ElementAt(i);
                Total_Kg_Rem = (int)(Double.Parse(head[2]) * (Current_PO_Bags - Int32.Parse(head[8])));
                if (Total_Kg_Rem < 0) Total_Kg_Rem = 0;
                temp = Decimal.Divide((Total_Kg_Rem - Int32.Parse(head[4])), 1000);
                if (temp < 0) temp = 0;
                Totes_Rem = (int)Math.Ceiling(temp);
                if (head[9] != "Not In Current PO") Heads[headName].Add(Totes_Rem.ToString());
            }

            Chart2 = chartPop("4AH2", Heads);
            Chart3 = chartPop("4AH3", Heads);
            Chart4 = chartPop("4AH4", Heads);
            Chart5 = chartPop("4AH5", Heads);

            ChartJson2 = JsonConvert.SerializeObject(Chart2, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, });
            ChartJson3 = JsonConvert.SerializeObject(Chart3, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, });
            ChartJson4 = JsonConvert.SerializeObject(Chart4, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, });
            ChartJson5 = JsonConvert.SerializeObject(Chart5, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, });
        }
        
    }
}

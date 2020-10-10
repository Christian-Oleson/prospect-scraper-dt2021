﻿using CsvHelper;
using HtmlAgilityPack;
using prospectScraper.DTOs;
using prospectScraper.Maps;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace prospectScraper
{
    public class ProspectScraper
    {
        public void RunTheBigBoards(bool parseDate)
        {
            File.WriteAllText($"logs{Path.DirectorySeparatorChar}Status.log", "");
            File.WriteAllText($"logs{Path.DirectorySeparatorChar}Prospects.log", "");

            Console.WriteLine("Getting data...");

            var webGet = new HtmlWeb
            {
                UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:31.0) Gecko/20100101 Firefox/31.0"
            };
            var document1 = webGet.Load("https://www.drafttek.com/2021-NFL-Draft-Big-Board/Top-NFL-Draft-Prospects-2021-Page-1.asp");
            var document2 = webGet.Load("https://www.drafttek.com/2021-NFL-Draft-Big-Board/Top-NFL-Draft-Prospects-2021-Page-2.asp");
            var document3 = webGet.Load("https://www.drafttek.com/2021-NFL-Draft-Big-Board/Top-NFL-Draft-Prospects-2021-Page-3.asp");
            var document4 = webGet.Load("https://www.drafttek.com/2021-NFL-Draft-Big-Board/Top-NFL-Draft-Prospects-2021-Page-4.asp");
            var document5 = webGet.Load("https://www.drafttek.com/2021-NFL-Draft-Big-Board/Top-NFL-Draft-Prospects-2021-Page-5.asp");

            Console.WriteLine("Parsing data...");

            //Get ranking date
            var dateOfRanks = document1.DocumentNode.SelectSingleNode("//*[@id='HeadlineInfo1']").InnerText.Replace(" EST", "").Trim();
            var parsedDate = ChangeDateStringToDateTime(dateOfRanks, parseDate);
            //Change date to proper date. The original format should be like this:
            //" May 21, 2019 2:00 AM EST"
            string dateInNiceFormat = parsedDate.ToString("yyyy-MM-dd");

            List<ProspectRanking> list1 = GetProspects(document1, parsedDate, 1);
            List<ProspectRanking> list2 = GetProspects(document2, parsedDate, 2);
            List<ProspectRanking> list3 = GetProspects(document3, parsedDate, 3);
            List<ProspectRanking> list4 = GetProspects(document4, parsedDate, 4);
            List<ProspectRanking> list5 = GetProspects(document5, parsedDate, 5);

            //This is the file name we are going to write.
            var csvFileName = $"ranks{Path.DirectorySeparatorChar}{dateInNiceFormat}-ranks.csv";

            Console.WriteLine("Creating csv...");

            //Write projects to csv with date.
            using (var writer = new StreamWriter(csvFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.Configuration.RegisterClassMap<ProspectRankingMap>();
                csv.WriteRecords(list1);
                csv.WriteRecords(list2);
                csv.WriteRecords(list3);
                if (list4.Count > 0)
                {
                    csv.WriteRecords(list4);
                }
                if (list5.Count > 0)
                {
                    csv.WriteRecords(list5);
                }
            }

            CheckForMismatches(csvFileName);
            CreateCombinedCSV();
            CheckForMismatches($"ranks{Path.DirectorySeparatorChar}combinedRanks2021.csv");
            CreateCombinedCSVWithExtras();

            Console.WriteLine("Big Board Completed.");
        }

        public void RunTheMockDraft(bool parseDate)
        {
            var webGet = new HtmlWeb
            {
                UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:31.0) Gecko/20100101 Firefox/31.0"
            };
            var document1 = webGet.Load("https://www.drafttek.com/2021-NFL-Mock-Draft/2021-NFL-Mock-Draft-Round-1.asp");
            var document3 = webGet.Load("https://www.drafttek.com/2021-NFL-Mock-Draft/2021-NFL-Mock-Draft-Round-2.asp");
            var document4 = webGet.Load("https://www.drafttek.com/2021-NFL-Mock-Draft/2021-NFL-Mock-Draft-Round-3.asp");
            var document5 = webGet.Load("https://www.drafttek.com/2021-NFL-Mock-Draft/2021-NFL-Mock-Draft-Round-4.asp");
            var document6 = webGet.Load("https://www.drafttek.com/2021-NFL-Mock-Draft/2021-NFL-Mock-Draft-Round-6.asp");

            // Need to get date of mock draft eventually.
            HtmlNode hn = document1.DocumentNode;
            HtmlNode hi1 = hn.SelectSingleNode("//*[@id='HeadlineInfo1']");
            DateTime mockDraftDate = ChangeDateStringToDateTime(hi1.InnerText.Replace(" EST", "").Trim(), parseDate);
            string draftDate = mockDraftDate.ToString("yyyy-MM-dd");


            List<MockDraftPick> list1 = GetMockDraft(document1, draftDate);
            List<MockDraftPick> list2 = GetMockDraft(webGet.Load("https://www.drafttek.com/2021-NFL-Mock-Draft/2021-NFL-Mock-Draft-Round-1b.asp"), draftDate);
            List<MockDraftPick> list3 = GetMockDraft(document3, draftDate);
            List<MockDraftPick> list4 = GetMockDraft(document4, draftDate);
            List<MockDraftPick> list5 = GetMockDraft(document5, draftDate);
            List<MockDraftPick> list6 = GetMockDraft(document6, draftDate);

            //This is the file name we are going to write.
            var csvFileName = $"mocks{Path.DirectorySeparatorChar}{draftDate}-mock.csv";

            Console.WriteLine("Creating csv...");

            //Write projects to csv with date.
            using (var writer = new StreamWriter(csvFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.Configuration.RegisterClassMap<MockDraftPickMap>();
                csv.WriteRecords(list1);
                csv.WriteRecords(list2);
                csv.WriteRecords(list3);
                csv.WriteRecords(list4);
                csv.WriteRecords(list5);
                csv.WriteRecords(list6);
            }

            Console.WriteLine("Checking for mock draft mismatches...");
            CheckForMockDraftMismatches(list1, "top of the first round");
            CheckForMockDraftMismatches(list2, "second half of the first round");
            CheckForMockDraftMismatches(list3, "second round");
            CheckForMockDraftMismatches(list4, "third round");
            CheckForMockDraftMismatches(list5, "fourth/fifth round");
            CheckForMockDraftMismatches(list6, "sixth/seventh round");

            CheckForMockDraftMismatches($"mocks{Path.DirectorySeparatorChar}{draftDate}-mock.csv");

            Console.WriteLine("Behold, the draft! Mock Draft Completed.");
        }

        public static DateTime ChangeDateStringToDateTime(string scrapedDate, bool parseDate)
        {
            //Change date to proper date. The original format should be like this:
            //" May 21, 2019 2:00 AM EST"
            bool parseWorks = DateTime.TryParse(scrapedDate, out DateTime parsedDate);
            string dateInNiceFormat = String.Empty;

            if (parseDate && parseWorks)
            {
                return parsedDate;
            }
            else
            {
                return DateTime.Now;
            }
        }

        public static List<MockDraftPick> GetMockDraft(HtmlDocument doc, string pickDate)
        {
            List<MockDraftPick> mdpList = new List<MockDraftPick>();
            // This is still messy from debugging the different values.  It should be optimized.
            var dn = doc.DocumentNode;
            var dns = dn.SelectNodes("/html/body/div/div/div/table");
            foreach (var node in dns)
            {
                bool hasTheStyle = node.Attributes.FirstOrDefault().Value.ToString().Contains("background-image: linear-gradient", StringComparison.OrdinalIgnoreCase);
                if (hasTheStyle)
                {
                    var tr = node.SelectSingleNode("tr");
                    MockDraftPick mockDraftPick = CreateMockDraftEntry(tr, pickDate);
                    mdpList.Add(mockDraftPick);
                }
            }
            return mdpList;
        }

        public static MockDraftPick CreateMockDraftEntry(HtmlNode tableRow, string pickDate)
        {
            var childNodes = tableRow.ChildNodes;
            var node1 = childNodes[1].InnerText; //pick number?
            string pickNumber = node1.Replace("\r", "")
                                    .Replace("\n", "")
                                    .Replace("\t", "")
                                    .Replace(" ", "");
            var node3 = childNodes[3]; //team (and team image)?
            var teamCity = node3.ChildNodes[0].InnerText
                                    .Replace("\r", "")
                                    .Replace("\n", "")
                                    .Replace("\t", "")
                                    .TrimEnd();
            var node5 = childNodes[5]; //Has Child Nodes - Player, School, Position, Reach/Value
            string playerName = node5.ChildNodes[1].InnerText
                                    .Replace("\r", "")
                                    .Replace("\n", "")
                                    .Replace("\t", "")
                                    .TrimEnd();
            string playerSchoolBeforeChecking = node5.ChildNodes[3].InnerText
                                    .Replace("\r", "")
                                    .Replace("\n", "")
                                    .Replace("\t", "")
                                    .TrimEnd(); // this may have a space afterwards.
            string playerSchool = School.CheckSchool(playerSchoolBeforeChecking);
            string playerPosition = node5.ChildNodes[5].InnerText
                                    .Replace("\r", "")
                                    .Replace("\n", "")
                                    .Replace("\t", "")
                                    .Replace(" ", "");
            string reachValue = node5.ChildNodes[9].InnerText
                                    .Replace("\r", "")
                                    .Replace("\n", "")
                                    .Replace("\t", "")
                                    .Replace(" ", "");

            var mdp = new MockDraftPick(pickNumber, teamCity, playerName, playerSchool, playerPosition, reachValue, pickDate);
            File.AppendAllText($"logs{Path.DirectorySeparatorChar}Prospects.log", "Mock Draft Round: " + mdp.round + Environment.NewLine);
            File.AppendAllText($"logs{Path.DirectorySeparatorChar}Prospects.log", "Pick Number: " + mdp.pickNumber + Environment.NewLine);
            File.AppendAllText($"logs{Path.DirectorySeparatorChar}Prospects.log", "Player: " + mdp.playerName + Environment.NewLine);
            return mdp;
        }

        public static string FormatDraftDate(string headlineInfo)
        {
            string hi2 = headlineInfo.Replace(" EST", "").Trim();
            // TODO: Change date to proper date. The original format should be like this:
            //" May 21, 2019 2:00 AM EST"
            bool parseWorks = DateTime.TryParse(hi2, out DateTime parsedDate);
            string dateInNiceFormat;
            if (parseWorks)
            {
                dateInNiceFormat = parsedDate.ToString("yyyy-MM-dd");
            }
            else
            {
                dateInNiceFormat = DateTime.Now.ToString("yyyy-MM-dd");
            }

            Console.WriteLine("Mock Draft - Date parsed: " + parsedDate + " - File name will be: " + dateInNiceFormat + "-mock.csv");
            return dateInNiceFormat;
        }

        private static void CreateCombinedCSV()
        {
            //Combine ranks from CSV files to create a master CSV.
            var filePaths = Directory.GetFiles($"ranks{Path.DirectorySeparatorChar}", "20??-??-??-ranks.csv").ToList<String>();
            //The results are probably already sorted, but I don't trust that, so I'm going to sort manually.
            filePaths.Sort();
            string destinationFile = $"ranks{Path.DirectorySeparatorChar}combinedRanks2021.csv";

            // Specify wildcard search to match CSV files that will be combined
            using var fileDest = new StreamWriter(destinationFile, false);
            for (int i = 0; i < filePaths.Count; i++)
            {
                string file = filePaths[i];

                string[] lines = File.ReadAllLines(file);

                if (i > 0)
                {
                    lines = lines.Skip(1).ToArray(); // Skip header row for all but first file
                }

                foreach (string line in lines)
                {
                    fileDest.WriteLine(line);
                }
            }
        }

        private static List<School> SchoolsAndConferencesFromCsv()
        {
            using var reader = new StreamReader($"info{Path.DirectorySeparatorChar}SchoolStatesAndConferences.csv");
            using var csv = new CsvReader(reader, CultureInfo.CurrentCulture);
            csv.Configuration.RegisterClassMap<SchoolCsvMap>();
            return csv.GetRecords<School>().ToList();
        }

        private static List<Region> StatesAndRegionsFromCsv()
        {
            using var reader = new StreamReader($"info{Path.DirectorySeparatorChar}StatesToRegions.csv");
            using var csv = new CsvReader(reader, CultureInfo.CurrentCulture);
            csv.Configuration.RegisterClassMap<RegionCsvMap>();
            return csv.GetRecords<Region>().ToList();
        }

        private static List<PositionType> PositionsAndTypesFromCsv()
        {
            using var reader = new StreamReader($"info{Path.DirectorySeparatorChar}PositionInfo.csv");
            using var csv = new CsvReader(reader, CultureInfo.CurrentCulture);
            csv.Configuration.RegisterClassMap<PositionTypeCsvMap>();
            return csv.GetRecords<PositionType>().ToList();
        }

        private static void CreateCombinedCSVWithExtras()
        {
            File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", "Creating the big CSV....." + Environment.NewLine);

            var schoolsAndConferences = SchoolsAndConferencesFromCsv();
            var statesAndRegions = StatesAndRegionsFromCsv();
            var positionsAndTypes = PositionsAndTypesFromCsv();

            // Let's assign these ranks point values.
            List<PointProjection> ranksToProjectedPoints;
            using (var reader = new StreamReader($"info{Path.DirectorySeparatorChar}RanksToProjectedPoints.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
            {
                csv.Configuration.RegisterClassMap<PointProjectionCsvMap>();
                ranksToProjectedPoints = csv.GetRecords<PointProjection>().ToList();
            }

            //Combine ranks from CSV files to create a master CSV.
            var filePaths = Directory.GetFiles($"ranks{Path.DirectorySeparatorChar}", "20??-??-??-ranks.csv").ToList<String>();
            //The results are probably already sorted, but I don't trust that, so I'm going to sort manually.
            filePaths.Sort();
            string destinationFile = $"ranks{Path.DirectorySeparatorChar}joinedRanks2021.csv";

            // Specify wildcard search to match CSV files that will be combined
            StreamWriter fileDest = new StreamWriter(destinationFile, false);

            for (int i = 0; i < filePaths.Count; i++)
            {
                string file = filePaths[i];

                string[] lines = File.ReadAllLines(file);

                if (i > 0)
                {
                    lines = lines.Skip(1).ToArray(); // Skip header row for all but first file
                }

                foreach (string line in lines)
                {
                    fileDest.WriteLine(line);
                }
            }

            fileDest.Close();

            // Get ranks from the newly created CSV file.
            List<ExistingProspectRanking> prospectRanks;
            using (var reader = new StreamReader($"ranks{Path.DirectorySeparatorChar}joinedRanks2021.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
            {
                csv.Configuration.RegisterClassMap<ExistingProspectRankingCsvMap>();
                prospectRanks = csv.GetRecords<ExistingProspectRanking>().ToList();
            }

            // Use linq to join the stuff back together, then write it out again.
            var combinedHistoricalRanks = from r in prospectRanks
                                          join school in schoolsAndConferences
                                            on r.school equals school.schoolName
                                          join region in statesAndRegions
                                            on school.state equals region.state
                                          join positions in positionsAndTypes
                                            on r.position1 equals positions.positionName
                                          join rank in ranksToProjectedPoints
                                            on r.rank equals rank.rank
                                          select new
                                          {
                                              Rank = r.rank,
                                              Change = r.change,
                                              Name = r.playerName,
                                              Position = r.position1,
                                              College = r.school,
                                              Conference = school.conference,
                                              State = school.state,
                                              Region = region.region,
                                              Height = r.height,
                                              Weight = r.weight,
                                              CollegeClass = r.collegeClass,
                                              PositionGroup = positions.positionGroup,
                                              PositionAspect = positions.positionAspect,
                                              ProspectStatus = r.draftStatus,
                                              Date = r.rankingDateString,
                                              Points = rank.projectedPoints
                                          };



            //Write everything back to CSV, only better!
            using (var writer = new StreamWriter($"ranks{Path.DirectorySeparatorChar}joinedRanks2021.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
            {
                csv.WriteRecords(combinedHistoricalRanks);
            }

            File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", "Creating the big CSV completed." + Environment.NewLine);
        }

        private static void CheckForMismatches(string csvFileName)
        {
            File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", "Checking for mismatches in " + csvFileName + "....." + Environment.NewLine);

            // Read in data from a different project.
            List<School> schoolsAndConferences;
            using (var reader = new StreamReader($"info{Path.DirectorySeparatorChar}SchoolStatesAndConferences.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
            {
                csv.Configuration.RegisterClassMap<SchoolCsvMap>();
                schoolsAndConferences = csv.GetRecords<School>().ToList();
            }

            List<ProspectRankSimple> ranks;
            using (var reader = new StreamReader(csvFileName))
            using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
            {
                csv.Configuration.RegisterClassMap<ProspectRankSimpleCsvMap>();
                ranks = csv.GetRecords<ProspectRankSimple>().ToList();
            }

            var schoolMismatches = Mismatches(ranks, schoolsAndConferences);

            bool noMismatches = true;

            if (schoolMismatches.Any())
            {
                File.WriteAllText($"logs{Path.DirectorySeparatorChar}Mismatches.log", "");
            }

            foreach (var s in schoolMismatches)
            {
                noMismatches = false;
                File.AppendAllText($"logs{Path.DirectorySeparatorChar}Mismatches.log", $"{s.Rank}, {s.Name}, {s.College}" + Environment.NewLine);
            }

            if (noMismatches)
            {
                File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", "No mismatches in " + csvFileName + "....." + Environment.NewLine);
            }
            else
            {
                File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", schoolMismatches.Count() + " mismatches in " + csvFileName + ".....Check Mismatches.log." + Environment.NewLine);
            }
        }

        public static List<ProspectRanking> GetProspects(HtmlDocument document, DateTime dateOfRanks, int pageNumber)
        {
            var prospectList = new List<ProspectRanking>();

            if (document.DocumentNode != null)
            {
                var tbl = document.DocumentNode.SelectNodes("/html[1]/body[1]/div[1]/div[3]/div[1]/table[1]");

                if (tbl == null)
                {
                    File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", $"No prospects on page {pageNumber}" + Environment.NewLine);
                    return prospectList;
                }

                // Create variables to store prospect rankings.
                int rank = 0;
                string change = "";
                string playerName = "";
                string school = "";
                string position1 = "";
                string height = "";
                int weight = 0;
                string collegeClass = "";

                foreach (HtmlNode table in tbl)
                {
                    foreach (HtmlNode row in table.SelectNodes("tr"))
                    {

                        foreach (HtmlNode cell in row.SelectNodes("th|td"))
                        {

                            string Xpath = cell.XPath;
                            int locationOfColumnNumber = cell.XPath.Length - 2;
                            char dataIndicator = Xpath[locationOfColumnNumber];
                            bool isRank = (dataIndicator == '1');
                            switch (dataIndicator)
                            {
                                case '1':
                                    // td[1]= Rank
                                    if (Int32.TryParse(cell.InnerText, out int rankNumber))
                                        rank = rankNumber;
                                    File.AppendAllText($"logs{Path.DirectorySeparatorChar}Prospects.log", "Big Board Rank: " + cell.InnerText + Environment.NewLine);
                                    break;
                                case '2':
                                    // td[2]= Change
                                    change = cell.InnerText;
                                    change = change.Replace("&nbsp;", "");
                                    break;
                                case '3':
                                    // td[3]= Player
                                    playerName = cell.InnerText;
                                    File.AppendAllText($"logs{Path.DirectorySeparatorChar}Prospects.log", "Player: " + cell.InnerText + Environment.NewLine);
                                    break;
                                case '4':
                                    // td[4]= School
                                    school = School.CheckSchool(cell.InnerText);
                                    break;
                                case '5':
                                    // td[5]= Pos1
                                    position1 = cell.InnerText;
                                    break;
                                case '6':
                                    // td[6]= Ht
                                    height = cell.InnerText;
                                    break;
                                case '7':
                                    // td[7]= Weight
                                    if (Int32.TryParse(cell.InnerText, out int weightNumber))
                                        weight = weightNumber;
                                    break;
                                case '8':
                                    // College Class- used to be Pos2 (which was often blank)
                                    collegeClass = cell.InnerText;
                                    break;
                                case '9':
                                    // td[9]= Link to Bio (not used)
                                    continue;
                                default:
                                    break;
                            }
                        }
                        // Handle draft eligibility and declarations (done via row color)
                        string draftStatus = "";
                        if (row.Attributes.Contains("style") && row.Attributes["style"].Value.Contains("background-color"))
                        {
                            string rowStyle = row.Attributes["style"].Value;
                            string backgroundColor = Regex.Match(rowStyle, @"background-color: \w*").Value.Substring(18);
                            draftStatus = backgroundColor switch
                            {
                                "white" => "Eligible",
                                "lightblue" => "Underclassman",
                                "palegoldenrod" => "Declared",
                                _ => "",
                            };
                            File.AppendAllText($"logs{Path.DirectorySeparatorChar}Prospects.log", "Draft Status: " + draftStatus + Environment.NewLine);
                        }
                        // The header is in the table, so I need to ignore it here.
                        if (change != "CNG")
                        {
                            prospectList.Add(new ProspectRanking(dateOfRanks, rank, change, playerName, school, position1, height, weight, collegeClass, draftStatus));
                        }
                    }
                }
                File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", $"Prospect count on page {pageNumber}: {prospectList.Count}" + Environment.NewLine);
            }
            return prospectList;
        }

        private static void CheckForMockDraftMismatches(List<MockDraftPick> listOfPicks, string description)
        {
            File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", $"Checking for mismatches in {description}...{Environment.NewLine}");

            var schoolMismatches = Mismatches(listOfPicks, SchoolsAndConferences());

            if (!schoolMismatches.Any())
            {
                File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", "No mismatches in " + description + "....." + Environment.NewLine);
            }
            else
            {
                File.WriteAllText($"logs{Path.DirectorySeparatorChar}Mismatches.log", "");
                foreach (var s in schoolMismatches)
                {
                    File.AppendAllText($"logs{Path.DirectorySeparatorChar}Mismatches.log", $"Mock draft mismatch: {s.Rank}, {s.Name}, {s.College}" + Environment.NewLine);
                }
                File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", schoolMismatches.Count + " mismatches in list of picks from " + description + ".....Check Mismatches.log." + Environment.NewLine);
            }
        }

        private static List<School> SchoolsAndConferences()
        {
            using var reader = new StreamReader($"info{Path.DirectorySeparatorChar}SchoolStatesAndConferences.csv");
            using var csv = new CsvReader(reader, CultureInfo.CurrentCulture);
            csv.Configuration.RegisterClassMap<SchoolCsvMap>();
            return csv.GetRecords<School>().ToList();
        }

        private static void CheckForMockDraftMismatches(string csvFileName)
        {
            File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", "Checking for mismatches in " + csvFileName + "....." + Environment.NewLine);

            var schoolsAndConferences = new List<School>();
            using (var reader = new StreamReader($"info{Path.DirectorySeparatorChar}SchoolStatesAndConferences.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
            {
                csv.Configuration.RegisterClassMap<SchoolCsvMap>();
                schoolsAndConferences = csv.GetRecords<School>().ToList();
            }

            List<MockDraftPick> ranks;
            using (var reader = new StreamReader(csvFileName))
            using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
            {
                csv.Configuration.RegisterClassMap<MockDraftPickCsvMap>();
                ranks = csv.GetRecords<MockDraftPick>().ToList();
            }

            var schoolMismatches = Mismatches(ranks, schoolsAndConferences);

            if (!schoolMismatches.Any())
            {
                foreach (var s in schoolMismatches)
                {
                    File.AppendAllText($"logs{Path.DirectorySeparatorChar}Mismatches.log", $"{s.Rank}, {s.Name}, {s.College}" + Environment.NewLine);
                }
                File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", $"No mismatches in {csvFileName}...{Environment.NewLine}");
            }
            else
            {
                File.AppendAllText($"logs{Path.DirectorySeparatorChar}Status.log", $"{schoolMismatches.Count} mismatches in {csvFileName}...Check Mismatches.log.{Environment.NewLine}");
            }
        }

        private static List<SchoolMismatchDTO> Mismatches(List<ProspectRankSimple> draftPicks, List<School> schools)
        {
            return (from r in draftPicks
                    join school in schools
                     on r.school equals school.schoolName into mm
                    from school in mm.DefaultIfEmpty()
                    where school is null
                    select new SchoolMismatchDTO()
                    {
                        Rank = r.rank.ToString(),
                        Name = r.playerName,
                        College = r.school
                    }).ToList();
        }

        private static List<SchoolMismatchDTO> Mismatches(List<MockDraftPick> draftPicks, List<School> schools)
        {
            return (from r in draftPicks
                    join school in schools
                        on r.school equals school.schoolName into mm
                    from school in mm.DefaultIfEmpty()
                    where school is null
                    select new SchoolMismatchDTO()
                    {
                        Rank = r.pickNumber,
                        Name = r.playerName,
                        College = r.school
                    }).ToList();
        }
    }
}

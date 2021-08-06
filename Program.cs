using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;


namespace ParseTextXcom
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamReader manufacture = new StreamReader(@"user\mods\XComFiles\Ruleset\manufacture_XCOMFILES.rul");
            StreamReader items = new StreamReader(@"user\mods\XComFiles\Ruleset\items_XCOMFILES.rul");
            StreamReader modEnUs = new StreamReader(@"user\mods\XComFiles\Language\en-US.yml");
            StreamReader baseEnUs = new StreamReader(@"standard\xcom1\Language\en-US.yml");

            var itemList = new List<Item>();
            var sellList = new List<SellPrice>();

            //Pull the sell price data
            string testLine = items.ReadLine().Trim();

            while (!items.EndOfStream)
            {
                //get name of item
                if (Regex.IsMatch(testLine, "- type: .*"))
                {
                    var itemName = testLine.Split(' ').Last();

                    //find sell price
                    testLine = items.ReadLine().Trim();
                    while (!items.EndOfStream && !Regex.IsMatch(testLine, "- type: .*"))
                    {
                        if (Regex.IsMatch(testLine, "costSell: .*"))
                        {
                            SellPrice tempSellPrice = new SellPrice()
                            {
                                name = itemName,
                                price = int.Parse(testLine.Split(' ').Last())
                            };
                            sellList.Add(tempSellPrice); // Only add if we have everything
                        }
                        testLine = items.ReadLine().Trim();
                    }

                }
                else
                {
                    testLine = items.ReadLine().Trim();
                }
            }

            //Build the localization database
            Dictionary<string, string> localization = new Dictionary<string, string>();
            testLine = modEnUs.ReadLine().Trim();
            while (!modEnUs.EndOfStream)
            {
                if (Regex.IsMatch(testLine, "STR_.*"))
                {
                    var key = testLine.Split(':').First();
                    var value = testLine.Split(':').Last().Trim();
                    if(!localization.ContainsKey(key)) localization.Add(key, value);
                }
                testLine = modEnUs.ReadLine().Trim();
            }

            testLine = baseEnUs.ReadLine().Trim();
            while (!baseEnUs.EndOfStream)
            {
                if (Regex.IsMatch(testLine, "STR_.*"))
                {
                    var key = testLine.Split(':').First();
                    var value = testLine.Split(':').Last().Trim();
                    if (!localization.ContainsKey(key)) localization.Add(key, value);
                }
                testLine = baseEnUs.ReadLine().Trim();
            }

            //Now pull the manufacturing data
            testLine = manufacture.ReadLine().Trim();

            while (!manufacture.EndOfStream)
            {
                if (Regex.IsMatch(testLine, "- name: .*"))
                {
                    localization.TryGetValue(testLine.Split(' ').Last(), out string tempLocalName);

                    Item currItem = new Item
                    {
                        name = testLine.Split(' ').Last(),
                        localName = tempLocalName
                    };
                    if (currItem.localName == null)
                    {
                        currItem.localName = currItem.name;
                    }

                    testLine = manufacture.ReadLine().Trim();
                    while (!manufacture.EndOfStream && !Regex.IsMatch(testLine, "- name: .*"))
                    {
                        if (Regex.IsMatch(testLine, "space: .*"))
                        {
                            currItem.space = int.Parse(testLine.Split(' ').Last());
                            currItem.totalSpace = currItem.space;
                        }
                        else if (Regex.IsMatch(testLine, "time: .*"))
                        {
                            currItem.time = int.Parse(testLine.Split(' ').Last());
                            currItem.totalTime = currItem.time;
                        }
                        else if (Regex.IsMatch(testLine, "cost: .*"))
                        {
                            currItem.cost = int.Parse(testLine.Split(' ').Last());
                            currItem.totalCost = currItem.cost;
                        }
                        else if (Regex.IsMatch(testLine, "requiredItems:"))
                        {
                            // Add in costs for components...  I'll make this data-driven later
                            testLine = manufacture.ReadLine().Trim();
                            while (Regex.IsMatch(testLine, "STR_.*"))
                            {
                                var tempSplit = testLine.Split(' ');
                                ExtraMat extraMat = new ExtraMat()
                                    { name = tempSplit.First(), qty = int.Parse(tempSplit.Last()) };

                                currItem.extraMats.Add(extraMat);

                                //if (Regex.IsMatch(testLine, "STR_ALIEN_ALLOYS: .*"))
                                //{
                                //    currItem.alloys = int.Parse(testLine.Split(' ').Last());
                                //    currItem.totalCost += currItem.alloys * 3000;
                                //    currItem.totalSpace += 10;
                                //    currItem.totalTime += currItem.alloys * 100;
                                //}
                                //else if (Regex.IsMatch(testLine, "STR_ELERIUM_115: .*"))
                                //{
                                //    currItem.elerium = int.Parse(testLine.Split(' ').Last());
                                //}
                                //else if (Regex.IsMatch(testLine, "STR_TOXIGUN: .*"))
                                //{
                                //    int tempVal = int.Parse(testLine.Split(' ').Last());
                                //    currItem.totalCost += tempVal * 57000;
                                //    currItem.totalSpace += 15;
                                //    currItem.totalTime += tempVal * 800;
                                //}
                                //else if (Regex.IsMatch(testLine, "STR_TOXIGUN_FLASK: .*"))
                                //{
                                //    int tempVal = int.Parse(testLine.Split(' ').Last());
                                //    currItem.totalCost += tempVal * 4500;
                                //    currItem.totalSpace += 7;
                                //    currItem.totalTime += tempVal * 120;
                                //}
                                //else if (Regex.IsMatch(testLine, "STR_DURATHREAD: .*"))
                                //{
                                //    int tempVal = int.Parse(testLine.Split(' ').Last());
                                //    currItem.totalCost += tempVal * 2000;
                                //    currItem.totalSpace += 10;
                                //    currItem.totalTime += tempVal * 60;
                                //}
                                //else if (Regex.IsMatch(testLine, "STR_PSICLONE: .*"))
                                //{
                                //    int tempVal = int.Parse(testLine.Split(' ').Last());
                                //    currItem.totalCost += tempVal * 42000;
                                //    currItem.totalSpace += 3;
                                //    currItem.totalTime += tempVal * 1000;
                                //}
                                //else if (Regex.IsMatch(testLine, "STR_XCOM_PSICLONE: .*"))
                                //{
                                //    int tempVal = int.Parse(testLine.Split(' ').Last());
                                //    currItem.totalCost += tempVal * 42100;
                                //    currItem.totalSpace += 5;
                                //    currItem.totalTime += tempVal * 1200;
                                //}
                                //else if (Regex.IsMatch(testLine, "STR_UFO_NAVIGATION: .*"))
                                //{
                                //    int tempVal = int.Parse(testLine.Split(' ').Last());
                                //    currItem.totalCost += tempVal * 159000;
                                //    currItem.totalSpace += (currItem.alloys > 0 ? 18 : 8);
                                //    currItem.totalTime += tempVal * 1900;
                                //}
                                //else currItem.other = true;

                                testLine = manufacture.ReadLine().Trim();
                            }
                        }

                        testLine = manufacture.ReadLine().Trim();
                    }
                    var rtnList = sellList.Where(x => x.name == currItem.name).ToArray();
                    if (rtnList.Length > 0)
                    {
                        currItem.sellPrice = rtnList.First().price;
                        itemList.Add(currItem);
                    }

                }

                else if (!manufacture.EndOfStream)
                    testLine = manufacture.ReadLine().Trim();

            }

            // At this point, we have a valid array of items, but without the
            // buildable materials costs factored in.  Let's fix that.

            using (StreamWriter fs = new StreamWriter(File.Create(@"ProfChrt.csv")))
            {
                fs.WriteLine(@"Name, Total Cost, Sell Price, Total Space, Total Time, Elerium, Non-Manufacturable Components");
                foreach (var i in itemList.OrderBy(x => x.localName))
                {
                    //if (i.elerium == 0)
                    fs.WriteLine($"{i.localName}, {i.totalCost}, {i.sellPrice}, {i.totalSpace}, {i.totalTime}, {i.elerium}, {i.other}");
                }
            }

            //            Console.ReadKey();
        }
    }

    public struct Item
    {
        public string name;
        public string localName;
        public int cost;
        public int time;
        public int space;
        public int alloys;
        public int elerium;
        public bool other;
        public int totalCost;
        public int totalTime;
        public int totalSpace;
        public int sellPrice;
        public List<ExtraMat> extraMats;
    }

    public struct SellPrice
    {
        public string name;
        public int price;
    }

    public struct ExtraMat
    {
        public string name;
        public int qty;
    }
}

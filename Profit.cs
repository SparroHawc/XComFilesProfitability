using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;


namespace ParseTextXcom
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamReader manufacture = new StreamReader(@"user\mods\XComFiles\Ruleset\manufacture_XCOMFILES.rul");
            StreamReader baseManufacture = new StreamReader(@"standard\xcom1\manufacture.rul");
            StreamReader items = new StreamReader(@"user\mods\XComFiles\Ruleset\items_XCOMFILES.rul");
            StreamReader baseItems = new StreamReader(@"standard\xcom1\items.rul");
            StreamReader modEnUs = new StreamReader(@"user\mods\XComFiles\Language\en-US.yml");
            StreamReader baseEnUs = new StreamReader(@"standard\xcom1\Language\en-US.yml");

            StreamReader[] manufactureReaders = { manufacture, baseManufacture };
            StreamReader[] itemsReaders = { items, baseItems };
            StreamReader[] langReaders = { modEnUs, baseEnUs };

            var itemList = new List<Item>();
            var sellList = new List<SellPrice>();

            string testLine;

            //Pull the sell price data
            foreach (StreamReader itemsReader in itemsReaders)
            { 
                testLine = itemsReader.ReadLine().Trim();

                while (!itemsReader.EndOfStream)
                {
                    //get name of item
                    if (Regex.IsMatch(testLine, "- type: .*"))
                    {
                        var itemName = testLine.Split(' ').Last();

                        //find sell price
                        testLine = itemsReader.ReadLine().Split('#').First().Trim();
                        while (!itemsReader.EndOfStream && !Regex.IsMatch(testLine, "- type: .*"))
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
                            testLine = itemsReader.ReadLine().Split('#').First().Trim();
                        }

                    }
                    else
                    {
                        testLine = itemsReader.ReadLine().Trim();
                    }
                }
            }

            //Build the localization database
            Dictionary<string, string> localization = new Dictionary<string, string>();
            foreach (StreamReader langReader in langReaders)
            { 
                testLine = langReader.ReadLine().Trim();
                while (!langReader.EndOfStream)
                {
                    if (Regex.IsMatch(testLine, "STR_.*"))
                    {
                        var key = testLine.Split(':').First();
                        var value = testLine.Split(':').Last().Trim();
                        if (!localization.ContainsKey(key)) localization.Add(key, value);
                    }
                    testLine = langReader.ReadLine().Trim();
                }
            }

            //Now pull the manufacturing data
            foreach(StreamReader manufactureReader in manufactureReaders)
            {
                testLine = manufactureReader.ReadLine().Trim();

                while (!manufactureReader.EndOfStream)
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

                        currItem.extraMats = new List<ExtraMat>();

                        testLine = manufactureReader.ReadLine().Trim();
                        while (!manufactureReader.EndOfStream && !Regex.IsMatch(testLine, "- name: .*"))
                        {
                            if (Regex.IsMatch(testLine, "space: .*"))
                            {
                                currItem.space = int.Parse(testLine.Split(' ').Last());
                                currItem.TotalSpace = currItem.space;
                            }
                            else if (Regex.IsMatch(testLine, "time: .*"))
                            {
                                currItem.time = int.Parse(testLine.Split(' ').Last());
                                currItem.TotalTime = currItem.time;
                            }
                            else if (Regex.IsMatch(testLine, "cost: .*"))
                            {
                                currItem.cost = int.Parse(testLine.Split(' ').Last());
                                currItem.TotalCost = currItem.cost;
                            }
                            else if (Regex.IsMatch(testLine, "requiredItems:"))
                            {
                                // Add in manufacturing components
                                testLine = manufactureReader.ReadLine().Trim();
                                while (!manufactureReader.EndOfStream && Regex.IsMatch(testLine, "STR_.*"))
                                {
                                    var tempSplit = testLine.Split(' ');
                                    // skip anything without a quantity
                                    if (Regex.IsMatch(tempSplit.Last(), "\\d+"))
                                    {
                                        ExtraMat extraMat = new ExtraMat()
                                        { name = tempSplit.First(), qty = int.Parse(tempSplit.Last()) };

                                        currItem.extraMats.Add(extraMat);
                                    }
                                    testLine = manufactureReader.ReadLine().Trim();
                                }
                            }

                            if(!manufactureReader.EndOfStream) testLine = manufactureReader.ReadLine().Trim();
                        }
                        var rtnList = sellList.Where(x => x.name == currItem.name).ToArray();
                        if (rtnList.Length > 0)
                        {
                            currItem.sellPrice = rtnList.First().price;
                            itemList.Add(currItem);
                        }

                    }

                    else if (!manufactureReader.EndOfStream)
                        testLine = manufactureReader.ReadLine().Trim();

                }
            }
            // At this point, we have a valid array of items, but without the
            // buildable materials costs factored in.  Let's fix that.

            bool matsClearedFlag = false;
            while (!matsClearedFlag)
            {
                matsClearedFlag = true;

                // Pick out the things that have extra mats so we can iterate through them
                List<Item> subList = itemList.Where(x => x.extraMats.Count > 0).ToList();
                for (int itemIdx = 0; itemIdx < subList.Count; itemIdx++)
                {
                    // Go through each mat and check if the item it points to is done being calculated; if so, add its totals.  Otherwise pass over it this loop. 
                    Item currItem = subList[itemIdx];

                    for (int matIdx = currItem.extraMats.Count - 1; matIdx >= 0; matIdx--)
                    {
                        ExtraMat currMat = currItem.extraMats[matIdx];
                        Item matsItem = itemList.Find(x => x.name == currMat.name.Trim(':'));
                        //Kick it out if we can't manufacture it
                        if (matsItem == null)
                        {
                            //Add up elerium cost
                            if (currMat.name.Trim(':') == "STR_ELERIUM_115")
                            {
                                currItem.elerium = currMat.qty;
                            }
                            currItem.other = true;
                            currItem.extraMats.Remove(currMat);
                            continue;
                        }
                        // Not everything in that mat is calculated, so we set the loop to go again and skip it
                        if (matsItem.extraMats.Count > 0)
                        {
                            matsClearedFlag = false;
                            continue;
                        }

                        Console.WriteLine("Added mats to " + currItem.name);
                        Console.WriteLine("Mat: " + matsItem.name);
                        Console.WriteLine("Qty: " + currMat.qty);
                        currItem.TotalCost += matsItem.TotalCost * currMat.qty;
                        currItem.TotalTime += matsItem.TotalTime * currMat.qty;
                        currItem.TotalSpace += matsItem.TotalSpace;
                        currItem.alloys += matsItem.alloys * currMat.qty;
                        currItem.elerium += matsItem.elerium * currMat.qty;
                        if(matsItem.other) currItem.other = true;
                        currItem.extraMats.Remove(currMat);

                    }
                }
            }

            // Buildable materials are done.

            using (StreamWriter fs = new StreamWriter(File.Create(@"ProfChrt.csv")))
            {
                fs.WriteLine(@"Name, Total Cost, Sell Price, Total Space, Total Time, Elerium, Non-Manufacturable Components");
                foreach (var i in itemList.OrderBy(x => x.localName))
                {
                    //if (i.elerium == 0)
                    fs.WriteLine($"{i.localName}, {i.TotalCost}, {i.sellPrice}, {i.TotalSpace}, {i.TotalTime}, {i.elerium}, {i.other}");
                }
            }

            // Console.ReadKey();
        }
    }

    public class Item
    {
        public string name;
        public string localName;
        public int cost;
        public int time;
        public int space;
        public int alloys;
        public int elerium;
        public bool other;
        public int sellPrice;

        public int TotalCost { get; set; }
        public int TotalTime { get; set; }
        public int TotalSpace { get; set; }

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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;

namespace WebScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            IDataParserFactory parserFactory;
            IJsonTransformerFactory transformerFactory = new JsonDataTransformerFactory();
            string htmlFilePath = @"C:\Users\ttahir\Downloads\Compressed\Task\Booking.com.html";

            Console.WriteLine("Enter 1 to Parse file, 2 to parse online booking.com url");
            int input = Convert.ToInt32(Console.ReadLine());
            if(input == 1)
            {
                parserFactory = new HtmlFileParserFactory();
            }
            else
            {
                parserFactory = new WebPageParserFactory();

            }
            
            var htmlParser = parserFactory.CreateParser();
            var jsonDataTransformer = transformerFactory.CreateTransformer();

            var extractedData = htmlParser.Parse(htmlFilePath);
            var jsonData = jsonDataTransformer.Transform(extractedData);
            Console.Write(jsonData);
            // Further processing or output of jsonData
        }
    }

    public interface IDataParser
    {
        ExtractedData Parse(string htmlFilePath);
    }

    public interface IJsonTransformer
    {
        string Transform(ExtractedData data);
    }

    public interface IDataParserFactory
    {
        IDataParser CreateParser();
    }

    public interface IJsonTransformerFactory
    {
        IJsonTransformer CreateTransformer();
    }

    public class HtmlFileParser : IDataParser
    {
        public ExtractedData Parse(string htmlFilePath)
        {
            var html = new HtmlDocument();
            html.Load(htmlFilePath);

            string hotelName = GetInnerText(html, "//*[@id='hp_hotel_name']");
            string address = GetInnerText(html, "//*[@id='hp_address_subtitle']/text()");
            string ratingStars = GetInnerText(html, "//*[@id='wrap-hotelpage-top']/h1/span[2]/span/i");

            string gainedPoints = GetInnerText(html,"//*[@id='js--hp-gallery-scorecard']/a/span[2]/span[1]");
            string totalReviewPoints = GetInnerText(html,"//*[@id='js--hp-gallery-scorecard']/a/span[2]/span[2]/span");
            string reviewPoints = gainedPoints + "/" + totalReviewPoints;
            string numberOfReviews = GetInnerText(html, "//*[@id='js--hp-gallery-scorecard']/span/strong");

            StringBuilder description = new StringBuilder();
            var descriptionParagraphs = html.DocumentNode.SelectNodes("//div[@id='summary']/p");
            foreach (var item in descriptionParagraphs)
            {
                description.AppendLine(Regex.Replace(item.InnerText, @"^\s+|\s+$|\n", string.Empty));
            }

            List<string> roomTypes = new List<string>();
            var roomTypesSection = html.DocumentNode.SelectNodes("//*[@id='maxotel_rooms']/tbody/tr");
            foreach (var item in roomTypesSection)
            {
                var name = item.SelectSingleNode("(td)[2]").InnerText;
                roomTypes.Add(Regex.Replace(name, @"^\s+|\s+$|\n", string.Empty));
            }

            var alternativeHotels = new List<(string name, string description)>();
            var alternativeHotelsSections = html.DocumentNode.SelectNodes("//*[@id='althotelsRow']/td");
            foreach (var item in alternativeHotelsSections)
            {
                string alternativeHotelName = item.SelectSingleNode($"{item.XPath}/p[@class='althotels-name']/a[@class='althotel_link']/text()").InnerText;
                string alternativeHotelDescription = item.SelectSingleNode($"{item.XPath}/span").InnerText;
                alternativeHotels.Add((name: Regex.Replace(alternativeHotelName, @"^\s+|\s+$|\n", string.Empty), description: Regex.Replace(alternativeHotelDescription, @"^\s+|\s+$|\n", string.Empty)));
            }

            return new ExtractedData
            {
                HotelName = hotelName,
                Address = address,
                RatingStars = ratingStars,
                ReviewPoints = reviewPoints,
                NumberOfReviews = numberOfReviews,
                Description = description.ToString(),
                RoomCategories = roomTypes,
                AlternativeHotels = alternativeHotels
            };
        }

        private string GetInnerText(HtmlDocument html, string xpath)
        {
            var node = html.DocumentNode.SelectSingleNode(xpath);
            return node.InnerText;
        }
    }


    public class WebPageParser : IDataParser
    {
        public ExtractedData Parse(string htmlFilePath)
        {
            var options = new ChromeOptions
            {
                BinaryLocation = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
            };

            options.AddArguments("headless");

            var chrome = new ChromeDriver(options);
            chrome.Navigate().GoToUrl("https://www.booking.com/hotel/de/kempinskibristolberlin.nl.html");

            var html = new HtmlDocument();
            html.LoadHtml(chrome.PageSource);

            string hotelName = GetInnerText(html, "//*[@id='hp_hotel_name']/div/h2");
            string address = GetInnerText(html, "//*[@id='showMap2']/span[1]");
            string ratingStars = "";
            string reviewPoints = GetInnerText(html, "//*[@id='js--hp-gallery-scorecard']/a/div/div/div/div[1]");
            string numberOfReviews = GetInnerText(html, "//*[@id='js--hp-gallery-scorecard']/a/div/div/div/div[2]/div[2]");
            string description = GetInnerText(html, "//*[@id='property_description_content']");
            List<string> roomTypes = new List<string>();
            var roomTypesSection = html.DocumentNode.SelectNodes("//*[@id=\"maxotelRoomArea\"]/section");
            var roomTypesList = roomTypesSection[0].ChildNodes.Skip(1);
            foreach (var item in roomTypesList)
            {
                var name = item.SelectSingleNode("div/div/a/span").InnerText;
                roomTypes.Add(name);
            }
            var alternativeHotels = new List<(string name, string description)>();
            return new ExtractedData
            {
                HotelName = hotelName,
                Address = address,
                RatingStars = ratingStars,
                ReviewPoints = reviewPoints,
                NumberOfReviews = numberOfReviews,
                Description = description,
                RoomCategories = roomTypes,
                AlternativeHotels= alternativeHotels
            };
        }

        private string GetInnerText(HtmlDocument html, string xpath)
        {
            var node = html.DocumentNode.SelectSingleNode(xpath);
            return node.InnerHtml;
        }
    }


    public class JsonDataTransformer : IJsonTransformer
    {
        public string Transform(ExtractedData data)
        {
            var myData = new
            {
                hotelName = Regex.Replace(data.HotelName, @"^\s+|\s+$|\n", string.Empty),
                address = Regex.Replace(data.Address, @"^\s+|\s+$|\n", string.Empty),
                ratingStars = Regex.Replace(data.RatingStars, @"^\s+|\s+$|\n", string.Empty),
                reviewPoints = Regex.Replace(data.ReviewPoints, @"^\s+|\s+$|\n", string.Empty),
                numberOfReviews = Regex.Replace(data.NumberOfReviews, @"^\s+|\s+$|\n", string.Empty),
                description = data.Description,
                roomCategories = data.RoomCategories,
                alternativeHotels = data.AlternativeHotels
            };

            return JsonConvert.SerializeObject(myData);
        }
    }

    public class HtmlFileParserFactory : IDataParserFactory
    {
        public IDataParser CreateParser()
        {
            return new HtmlFileParser();
        }
    }

    public class WebPageParserFactory : IDataParserFactory
    {
        public IDataParser CreateParser()
        {
            return new WebPageParser();
        }
    }

    public class JsonDataTransformerFactory : IJsonTransformerFactory
    {
        public IJsonTransformer CreateTransformer()
        {
            return new JsonDataTransformer();
        }
    }

    public class ExtractedData
    {
        public string HotelName { get; set; }
        public string Address { get; set; }
        public string RatingStars { get; set; }
        public string ReviewPoints { get; set; }
        public string NumberOfReviews { get; set; }
        public string Description { get; set; }
        public List<string> RoomCategories { get; set; }
        public List<(string name, string description)> AlternativeHotels { get; set; }
    }

}

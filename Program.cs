﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;



namespace WebScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            string htmlFilePath = @"C:\Users\ttahir\Downloads\Compressed\Task\Booking.com.html";

            IDataParserFactory parserFactory = new HtmlParserFactory();
            IJsonTransformerFactory transformerFactory = new JsonDataTransformerFactory();

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

    public class HtmlParser : IDataParser
    {
        public ExtractedData Parse(string htmlFilePath)
        {
            var html = new HtmlDocument();
            html.Load(htmlFilePath);

            string hotelName = GetInnerText(html, "//*[@id='hp_hotel_name']");
            string address = GetInnerText(html, "//*[@id='hp_address_subtitle']/text()");
            string ratingStars = GetInnerText(html, "//*[@id='wrap-hotelpage-top']/h1/span[2]/span/i");
            string reviewPoints = GetInnerText(html, "//*[@id='js--hp-gallery-scorecard']/a/span[2]");
            string numberOfReviews = GetInnerText(html, "//*[@id='js--hp-gallery-scorecard']/span/strong");

            StringBuilder description = new StringBuilder();
            var descriptionParagraphs = html.DocumentNode.SelectNodes("//div[@id='summary']/p");
            foreach (var item in descriptionParagraphs)
            {
                description.Append(item.InnerText);
            }

            List<string> roomTypes = new List<string>();
            var roomTypesSection = html.DocumentNode.SelectNodes("//*[@id='maxotel_rooms']/tbody/tr");
            foreach (var item in roomTypesSection)
            {
                var name = item.SelectSingleNode("(td)[2]").InnerText;
                roomTypes.Add(name);
            }

            List<string> alternativeHotels = new List<string>();
            var alternativeHotelsSections = html.DocumentNode.SelectNodes("//*[@id='althotelsRow']/td");
            foreach (var item in alternativeHotelsSections)
            {
                string alternativeHotelName = item.SelectSingleNode($"{item.XPath}/p[@class='althotels-name']/a[@class='althotel_link']/text()").InnerText;
                alternativeHotels.Add(alternativeHotelName);
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
            return node?.InnerText?.Replace(" ", "");
        }
    }

    public class JsonDataTransformer : IJsonTransformer
    {
        public string Transform(ExtractedData data)
        {
            var myData = new
            {
                hotelName = Regex.Replace(data.HotelName, @"\s+", string.Empty),
                address = Regex.Replace(data.Address, @"\s+", string.Empty),
                ratingStars = Regex.Replace(data.RatingStars, @"\s+", string.Empty),
                reviewPoints = Regex.Replace(data.ReviewPoints, @"\s+", string.Empty),
                numberOfReviews = Regex.Replace(data.NumberOfReviews, @"\s+", string.Empty),
                description = Regex.Replace(data.Description, @"\s+", string.Empty),
                roomCategories = data.RoomCategories,
                alternativeHotels = data.AlternativeHotels
            };

            return JsonConvert.SerializeObject(myData);
        }
    }

    public class HtmlParserFactory : IDataParserFactory
    {
        public IDataParser CreateParser()
        {
            return new HtmlParser();
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
        public List<string> AlternativeHotels { get; set; }
    }

}
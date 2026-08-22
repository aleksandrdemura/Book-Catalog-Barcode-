// BookCatalog.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BarcodeLib;

class Book
{
    [JsonPropertyName("title")]
    public string Title { get; set; }
    [JsonPropertyName("author")]
    public string Author { get; set; }
    [JsonPropertyName("isbn")]
    public string ISBN { get; set; }
    [JsonPropertyName("year")]
    public int Year { get; set; }
    [JsonPropertyName("publisher")]
    public string Publisher { get; set; }
}

class Catalog
{
    [JsonPropertyName("books")]
    public List<Book> Books { get; set; } = new List<Book>();
}

class BookCatalog
{
    private static readonly string DataFile = "books.json";
    private static readonly string BarcodeDir = "barcodes";
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: BookCatalog <command> [options]");
            return;
        }
        var catalog = LoadCatalog();
        string cmd = args[0];
        switch (cmd)
        {
            case "add":
                if (args.Length < 6) { Console.WriteLine("add <title> <author> <isbn> <year> <publisher>"); return; }
                AddBook(catalog, args[1], args[2], args[3], int.Parse(args[4]), args[5]);
                break;
            case "list":
                ListBooks(catalog);
                break;
            case "search":
                if (args.Length < 2) { Console.WriteLine("search <term>"); return; }
                SearchBooks(catalog, args[1]);
                break;
            case "show":
                if (args.Length < 2) { Console.WriteLine("show <isbn>"); return; }
                ShowBook(catalog, args[1]);
                break;
            case "barcode":
                if (args.Length < 2) { Console.WriteLine("barcode <isbn>"); return; }
                GenerateBarcode(catalog, args[1]);
                break;
            default:
                Console.WriteLine("Unknown command");
                break;
        }
    }

    static Catalog LoadCatalog()
    {
        if (!File.Exists(DataFile)) return new Catalog();
        string json = File.ReadAllText(DataFile);
        return JsonSerializer.Deserialize<Catalog>(json) ?? new Catalog();
    }

    static void SaveCatalog(Catalog catalog)
    {
        string json = JsonSerializer.Serialize(catalog, Options);
        File.WriteAllText(DataFile, json);
    }

    static void AddBook(Catalog catalog, string title, string author, string isbn, int year, string publisher)
    {
        if (catalog.Books.Any(b => b.ISBN == isbn))
        {
            Console.WriteLine($"Book with ISBN {isbn} already exists.");
            return;
        }
        catalog.Books.Add(new Book { Title = title, Author = author, ISBN = isbn, Year = year, Publisher = publisher });
        SaveCatalog(catalog);
        Console.WriteLine($"✅ Added: \"{title}\" by {author} (ISBN: {isbn})");
    }

    static void ListBooks(Catalog catalog)
    {
        if (!catalog.Books.Any())
        {
            Console.WriteLine("No books in catalog.");
            return;
        }
        Console.WriteLine("\n📋 All Books:");
        for (int i = 0; i < catalog.Books.Count; i++)
        {
            var b = catalog.Books[i];
            Console.WriteLine($"{i+1}. {b.Title} ({b.ISBN}) – {b.Author} ({b.Year})");
        }
    }

    static void SearchBooks(Catalog catalog, string term)
    {
        var lower = term.ToLower();
        var results = catalog.Books.Where(b =>
            b.Title.ToLower().Contains(lower) ||
            b.Author.ToLower().Contains(lower) ||
            b.ISBN.Contains(term)
        ).ToList();
        if (!results.Any())
        {
            Console.WriteLine("No matching books.");
            return;
        }
        Console.WriteLine($"\n🔍 Search results for \"{term}\":");
        foreach (var b in results)
            Console.WriteLine($"{b.Title} – {b.Author} ({b.ISBN})");
    }

    static void ShowBook(Catalog catalog, string isbn)
    {
        var book = catalog.Books.FirstOrDefault(b => b.ISBN == isbn);
        if (book == null)
        {
            Console.WriteLine($"Book with ISBN {isbn} not found.");
            return;
        }
        Console.WriteLine($"\n📖 Details for {isbn}:");
        Console.WriteLine($"Title: {book.Title}");
        Console.WriteLine($"Author: {book.Author}");
        Console.WriteLine($"ISBN: {book.ISBN}");
        Console.WriteLine($"Year: {book.Year}");
        Console.WriteLine($"Publisher: {book.Publisher}");
        string barcodePath = Path.Combine(BarcodeDir, isbn + ".png");
        if (File.Exists(barcodePath))
            Console.WriteLine($"Barcode: {barcodePath}");
        else
            Console.WriteLine("Barcode not generated yet.");
    }

    static void GenerateBarcode(Catalog catalog, string isbn)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(isbn, @"^\d{13}$"))
        {
            Console.WriteLine("Invalid ISBN. Must be 13 digits.");
            return;
        }
        if (!catalog.Books.Any(b => b.ISBN == isbn))
        {
            Console.WriteLine($"Book with ISBN {isbn} not found.");
            return;
        }
        Directory.CreateDirectory(BarcodeDir);
        try
        {
            BarcodeLib.Barcode barcode = new BarcodeLib.Barcode();
            System.Drawing.Image img = barcode.Encode(TYPE.EAN13, isbn, System.Drawing.Color.Black, System.Drawing.Color.White, 300, 100);
            string filePath = Path.Combine(BarcodeDir, isbn + ".png");
            img.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            Console.WriteLine($"✅ Barcode generated: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating barcode: {ex.Message}");
        }
    }
}

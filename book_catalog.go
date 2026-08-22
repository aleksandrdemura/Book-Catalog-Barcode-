// book_catalog.go
package main

import (
	"encoding/json"
	"flag"
	"fmt"
	"image/png"
	"os"
	"strconv"
	"strings"

	"github.com/boombuler/barcode"
	"github.com/boombuler/barcode/ean"
)

type Book struct {
	Title     string `json:"title"`
	Author    string `json:"author"`
	ISBN      string `json:"isbn"`
	Year      int    `json:"year"`
	Publisher string `json:"publisher"`
}

type Catalog struct {
	Books []Book `json:"books"`
	File  string
}

func NewCatalog(file string) *Catalog {
	c := &Catalog{File: file}
	c.load()
	return c
}

func (c *Catalog) load() {
	data, err := os.ReadFile(c.File)
	if err != nil {
		return
	}
	json.Unmarshal(data, c)
}

func (c *Catalog) save() {
	data, _ := json.MarshalIndent(c, "", "  ")
	os.WriteFile(c.File, data, 0644)
}

func (c *Catalog) Add(title, author, isbn string, year int, publisher string) {
	for _, b := range c.Books {
		if b.ISBN == isbn {
			fmt.Printf("Book with ISBN %s already exists.\n", isbn)
			return
		}
	}
	book := Book{Title: title, Author: author, ISBN: isbn, Year: year, Publisher: publisher}
	c.Books = append(c.Books, book)
	c.save()
	fmt.Printf("✅ Added: \"%s\" by %s (ISBN: %s)\n", title, author, isbn)
}

func (c *Catalog) List() {
	if len(c.Books) == 0 {
		fmt.Println("No books in catalog.")
		return
	}
	fmt.Println("\n📋 All Books:")
	for i, b := range c.Books {
		fmt.Printf("%d. %s (%s) – %s (%d)\n", i+1, b.Title, b.ISBN, b.Author, b.Year)
	}
}

func (c *Catalog) Search(term string) {
	termLower := strings.ToLower(term)
	var results []Book
	for _, b := range c.Books {
		if strings.Contains(strings.ToLower(b.Title), termLower) ||
			strings.Contains(strings.ToLower(b.Author), termLower) ||
			strings.Contains(b.ISBN, term) {
			results = append(results, b)
		}
	}
	if len(results) == 0 {
		fmt.Println("No matching books.")
		return
	}
	fmt.Printf("\n🔍 Search results for \"%s\":\n", term)
	for _, b := range results {
		fmt.Printf("%s – %s (%s)\n", b.Title, b.Author, b.ISBN)
	}
}

func (c *Catalog) Show(isbn string) {
	for _, b := range c.Books {
		if b.ISBN == isbn {
			fmt.Printf("\n📖 Details for %s:\n", isbn)
			fmt.Printf("Title: %s\n", b.Title)
			fmt.Printf("Author: %s\n", b.Author)
			fmt.Printf("ISBN: %s\n", b.ISBN)
			fmt.Printf("Year: %d\n", b.Year)
			fmt.Printf("Publisher: %s\n", b.Publisher)
			barcodePath := "barcodes/" + isbn + ".png"
			if _, err := os.Stat(barcodePath); err == nil {
				fmt.Printf("Barcode: %s\n", barcodePath)
			} else {
				fmt.Println("Barcode not generated yet.")
			}
			return
		}
	}
	fmt.Printf("Book with ISBN %s not found.\n", isbn)
}

func (c *Catalog) GenerateBarcode(isbn string) {
	if len(isbn) != 13 || !isNumeric(isbn) {
		fmt.Println("Invalid ISBN. Must be 13 digits.")
		return
	}
	// Check if book exists
	found := false
	for _, b := range c.Books {
		if b.ISBN == isbn {
			found = true
			break
		}
	}
	if !found {
		fmt.Printf("Book with ISBN %s not found.\n", isbn)
		return
	}
	// Generate barcode
	os.MkdirAll("barcodes", 0755)
	eanCode, err := ean.Encode(isbn)
	if err != nil {
		fmt.Printf("Error generating barcode: %v\n", err)
		return
	}
	// Scale for better visibility
	eanCode, _ = barcode.Scale(eanCode, 300, 100)
	file, err := os.Create("barcodes/" + isbn + ".png")
	if err != nil {
		fmt.Printf("Error creating file: %v\n", err)
		return
	}
	defer file.Close()
	err = png.Encode(file, eanCode)
	if err != nil {
		fmt.Printf("Error encoding PNG: %v\n", err)
		return
	}
	fmt.Printf("✅ Barcode generated: barcodes/%s.png\n", isbn)
}

func isNumeric(s string) bool {
	for _, c := range s {
		if c < '0' || c > '9' {
			return false
		}
	}
	return true
}

func main() {
	if len(os.Args) < 2 {
		fmt.Println("Usage: book_catalog <command> [options]")
		return
	}
	catalog := NewCatalog("books.json")
	cmd := os.Args[1]

	switch cmd {
	case "add":
		if len(os.Args) < 7 {
			fmt.Println("add <title> <author> <isbn> <year> <publisher>")
			return
		}
		title := os.Args[2]
		author := os.Args[3]
		isbn := os.Args[4]
		year, _ := strconv.Atoi(os.Args[5])
		publisher := os.Args[6]
		catalog.Add(title, author, isbn, year, publisher)
	case "list":
		catalog.List()
	case "search":
		if len(os.Args) < 3 {
			fmt.Println("search <term>")
			return
		}
		catalog.Search(os.Args[2])
	case "show":
		if len(os.Args) < 3 {
			fmt.Println("show <isbn>")
			return
		}
		catalog.Show(os.Args[2])
	case "barcode":
		if len(os.Args) < 3 {
			fmt.Println("barcode <isbn>")
			return
		}
		catalog.GenerateBarcode(os.Args[2])
	default:
		fmt.Println("Unknown command")
	}
}

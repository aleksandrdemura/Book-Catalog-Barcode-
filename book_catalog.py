# book_catalog.py
import json
import os
import sys
import argparse
import barcode
from barcode.writer import ImageWriter
from PIL import Image

DATA_FILE = "books.json"
BARCODE_DIR = "barcodes"

class Book:
    def __init__(self, title, author, isbn, year, publisher):
        self.title = title
        self.author = author
        self.isbn = isbn
        self.year = year
        self.publisher = publisher

    def to_dict(self):
        return {
            "title": self.title,
            "author": self.author,
            "isbn": self.isbn,
            "year": self.year,
            "publisher": self.publisher
        }

    @classmethod
    def from_dict(cls, data):
        return cls(data["title"], data["author"], data["isbn"], data["year"], data["publisher"])

class Catalog:
    def __init__(self):
        self.books = []
        self.load()

    def load(self):
        if os.path.exists(DATA_FILE):
            with open(DATA_FILE, "r") as f:
                data = json.load(f)
                self.books = [Book.from_dict(b) for b in data]

    def save(self):
        with open(DATA_FILE, "w") as f:
            json.dump([b.to_dict() for b in self.books], f, indent=2)

    def add(self, title, author, isbn, year, publisher):
        # Check for duplicate ISBN
        for b in self.books:
            if b.isbn == isbn:
                print(f"Book with ISBN {isbn} already exists.")
                return
        book = Book(title, author, isbn, year, publisher)
        self.books.append(book)
        self.save()
        print(f"✅ Added: \"{title}\" by {author} (ISBN: {isbn})")

    def list(self):
        if not self.books:
            print("No books in catalog.")
            return
        print("\n📋 All Books:")
        for i, b in enumerate(self.books, 1):
            print(f"{i}. {b.title} ({b.isbn}) – {b.author} ({b.year})")

    def search(self, term):
        term_lower = term.lower()
        results = []
        for b in self.books:
            if (term_lower in b.title.lower() or
                term_lower in b.author.lower() or
                term_lower in b.isbn):
                results.append(b)
        if not results:
            print("No matching books.")
            return
        print(f"\n🔍 Search results for \"{term}\":")
        for b in results:
            print(f"{b.title} – {b.author} ({b.isbn})")

    def show(self, isbn):
        for b in self.books:
            if b.isbn == isbn:
                print(f"\n📖 Details for {isbn}:")
                print(f"Title: {b.title}")
                print(f"Author: {b.author}")
                print(f"ISBN: {b.isbn}")
                print(f"Year: {b.year}")
                print(f"Publisher: {b.publisher}")
                # Check if barcode exists
                barcode_path = os.path.join(BARCODE_DIR, f"{isbn}.png")
                if os.path.exists(barcode_path):
                    print(f"Barcode: {barcode_path}")
                else:
                    print("Barcode not generated yet.")
                return
        print(f"Book with ISBN {isbn} not found.")

    def generate_barcode(self, isbn):
        # Validate ISBN (EAN-13)
        if len(isbn) != 13 or not isbn.isdigit():
            print("Invalid ISBN. Must be 13 digits.")
            return
        # Check if book exists
        book = None
        for b in self.books:
            if b.isbn == isbn:
                book = b
                break
        if not book:
            print(f"Book with ISBN {isbn} not found.")
            return
        # Create barcode directory
        os.makedirs(BARCODE_DIR, exist_ok=True)
        try:
            # EAN-13 barcode writer
            ean = barcode.get_barcode_class('ean13')
            ean_code = ean(isbn, writer=ImageWriter())
            filename = ean_code.save(os.path.join(BARCODE_DIR, isbn))
            print(f"✅ Barcode generated: {filename}")
        except Exception as e:
            print(f"Error generating barcode: {e}")

def main():
    parser = argparse.ArgumentParser(description="Book Catalog with Barcode")
    subparsers = parser.add_subparsers(dest="cmd", required=True)

    add_parser = subparsers.add_parser("add")
    add_parser.add_argument("title")
    add_parser.add_argument("author")
    add_parser.add_argument("isbn")
    add_parser.add_argument("year", type=int)
    add_parser.add_argument("publisher")

    subparsers.add_parser("list")

    search_parser = subparsers.add_parser("search")
    search_parser.add_argument("term")

    show_parser = subparsers.add_parser("show")
    show_parser.add_argument("isbn")

    barcode_parser = subparsers.add_parser("barcode")
    barcode_parser.add_argument("isbn")

    args = parser.parse_args()
    catalog = Catalog()

    if args.cmd == "add":
        catalog.add(args.title, args.author, args.isbn, args.year, args.publisher)
    elif args.cmd == "list":
        catalog.list()
    elif args.cmd == "search":
        catalog.search(args.term)
    elif args.cmd == "show":
        catalog.show(args.isbn)
    elif args.cmd == "barcode":
        catalog.generate_barcode(args.isbn)

if __name__ == "__main__":
    main()

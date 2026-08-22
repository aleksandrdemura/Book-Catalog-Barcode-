#📚 Book Catalog (Barcode) — Multi‑Language Library Manager
8 languages, one powerful catalog – manage your book collection, generate barcodes for ISBN, and search by title, author, or ISBN – right from your terminal.

✨ Features
📖 Add books – title, author, ISBN‑13, year, publisher

🔢 Barcode generation – create EAN‑13 barcode images for any ISBN

📋 List all books – view your entire catalog

🔍 Search – by title, author, or ISBN (partial or exact)

📄 Book details – show full info with barcode image path

💾 Persistent storage – all data saved in books.json

🖼️ Barcode images – saved as PNG files (named by ISBN)

🧰 Supported Languages & Dependencies
Language	File	Dependencies (for barcode generation)
Python	book_catalog.py	python-barcode, Pillow
Go	book_catalog.go	github.com/boombuler/barcode
JavaScript (Node)	book_catalog.js	bwip-js, canvas
Ruby	book_catalog.rb	barby, chunky_png
PHP	book_catalog.php	php-barcode-generator (composer)
Java	BookCatalog.java	barcode4j (or zxing)
C#	BookCatalog.cs	BarcodeLib (NuGet)
C++	book_catalog.cpp	libzint (or ZXing C++ port)
🚀 Quick Start
All implementations follow the same CLI pattern:

bash
# Add a book
<command> add "The Great Gatsby" "F. Scott Fitzgerald" "9780743273565" 1925 "Scribner"

# List all books
<command> list

# Search for books
<command> search "Gatsby"

# Show details of a book (by ISBN)
<command> show 9780743273565

# Generate barcode for a book (saves as <isbn>.png)
<command> barcode 9780743273565
Commands:

add <title> <author> <isbn> <year> <publisher> – add a book

list – show all books

search <term> – search by title, author, or ISBN

show <isbn> – display full details

barcode <isbn> – generate and save barcode image

📸 Example Output
text
📚 Book Catalog
Added: "The Great Gatsby" by F. Scott Fitzgerald (ISBN: 9780743273565)

📋 All Books:
1. The Great Gatsby (9780743273565) – F. Scott Fitzgerald (1925)

🔍 Search results for "Gatsby":
The Great Gatsby – F. Scott Fitzgerald (9780743273565)

📖 Details for 9780743273565:
Title: The Great Gatsby
Author: F. Scott Fitzgerald
ISBN: 9780743273565
Year: 1925
Publisher: Scribner
Barcode: 9780743273565.png
📁 Repository Structure
text
.
├── README.md
├── python/
│   └── book_catalog.py
├── go/
│   └── book_catalog.go
├── javascript/
│   └── book_catalog.js
├── ruby/
│   └── book_catalog.rb
├── php/
│   └── book_catalog.php
├── java/
│   └── BookCatalog.java
├── csharp/
│   └── BookCatalog.cs
└── cpp/
    └── book_catalog.cpp

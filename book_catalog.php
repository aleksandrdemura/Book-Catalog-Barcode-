# book_catalog.php
#!/usr/bin/env php
<?php

require_once 'vendor/autoload.php'; // for barcode generator

use Picqer\Barcode\BarcodeGeneratorPNG;

$DATA_FILE = 'books.json';
$BARCODE_DIR = 'barcodes';

class Book {
    public $title, $author, $isbn, $year, $publisher;
    function __construct($title, $author, $isbn, $year, $publisher) {
        $this->title = $title;
        $this->author = $author;
        $this->isbn = $isbn;
        $this->year = $year;
        $this->publisher = $publisher;
    }
    function toArray() {
        return ['title' => $this->title, 'author' => $this->author, 'isbn' => $this->isbn, 'year' => $this->year, 'publisher' => $this->publisher];
    }
    static function fromArray($data) {
        return new self($data['title'], $data['author'], $data['isbn'], $data['year'], $data['publisher']);
    }
}

class Catalog {
    private $books = [];
    private $file;

    function __construct($file) {
        $this->file = $file;
        $this->load();
    }

    function load() {
        if (file_exists($this->file)) {
            $data = json_decode(file_get_contents($this->file), true);
            foreach ($data as $item) {
                $this->books[] = Book::fromArray($item);
            }
        }
    }

    function save() {
        $data = array_map(function($b) { return $b->toArray(); }, $this->books);
        file_put_contents($this->file, json_encode($data, JSON_PRETTY_PRINT));
    }

    function add($title, $author, $isbn, $year, $publisher) {
        foreach ($this->books as $b) {
            if ($b->isbn == $isbn) {
                echo "Book with ISBN $isbn already exists.\n";
                return;
            }
        }
        $book = new Book($title, $author, $isbn, $year, $publisher);
        $this->books[] = $book;
        $this->save();
        echo "✅ Added: \"$title\" by $author (ISBN: $isbn)\n";
    }

    function list() {
        if (empty($this->books)) {
            echo "No books in catalog.\n";
            return;
        }
        echo "\n📋 All Books:\n";
        foreach ($this->books as $i => $b) {
            echo ($i+1) . ". {$b->title} ({$b->isbn}) – {$b->author} ({$b->year})\n";
        }
    }

    function search($term) {
        $term = strtolower($term);
        $results = array_filter($this->books, function($b) use ($term) {
            return strpos(strtolower($b->title), $term) !== false ||
                   strpos(strtolower($b->author), $term) !== false ||
                   strpos($b->isbn, $term) !== false;
        });
        if (empty($results)) {
            echo "No matching books.\n";
            return;
        }
        echo "\n🔍 Search results for \"$term\":\n";
        foreach ($results as $b) {
            echo "{$b->title} – {$b->author} ({$b->isbn})\n";
        }
    }

    function show($isbn) {
        foreach ($this->books as $b) {
            if ($b->isbn == $isbn) {
                echo "\n📖 Details for $isbn:\n";
                echo "Title: {$b->title}\n";
                echo "Author: {$b->author}\n";
                echo "ISBN: {$b->isbn}\n";
                echo "Year: {$b->year}\n";
                echo "Publisher: {$b->publisher}\n";
                $barcodePath = BARCODE_DIR . '/' . $isbn . '.png';
                if (file_exists($barcodePath)) {
                    echo "Barcode: $barcodePath\n";
                } else {
                    echo "Barcode not generated yet.\n";
                }
                return;
            }
        }
        echo "Book with ISBN $isbn not found.\n";
    }

    function generateBarcode($isbn) {
        if (!preg_match('/^\d{13}$/', $isbn)) {
            echo "Invalid ISBN. Must be 13 digits.\n";
            return;
        }
        $found = false;
        foreach ($this->books as $b) {
            if ($b->isbn == $isbn) { $found = true; break; }
        }
        if (!$found) {
            echo "Book with ISBN $isbn not found.\n";
            return;
        }
        if (!is_dir(BARCODE_DIR)) mkdir(BARCODE_DIR, 0755, true);
        try {
            $generator = new BarcodeGeneratorPNG();
            $png = $generator->getBarcode($isbn, $generator::TYPE_EAN_13);
            file_put_contents(BARCODE_DIR . '/' . $isbn . '.png', $png);
            echo "✅ Barcode generated: " . BARCODE_DIR . '/' . $isbn . ".png\n";
        } catch (Exception $e) {
            echo "Error generating barcode: " . $e->getMessage() . "\n";
        }
    }
}

if ($argc < 2) {
    die("Usage: php book_catalog.php <command> [options]\n");
}
$catalog = new Catalog($DATA_FILE);
$cmd = $argv[1];

switch ($cmd) {
    case 'add':
        if ($argc < 7) die("add <title> <author> <isbn> <year> <publisher>\n");
        $catalog->add($argv[2], $argv[3], $argv[4], (int)$argv[5], $argv[6]);
        break;
    case 'list':
        $catalog->list();
        break;
    case 'search':
        if ($argc < 3) die("search <term>\n");
        $catalog->search($argv[2]);
        break;
    case 'show':
        if ($argc < 3) die("show <isbn>\n");
        $catalog->show($argv[2]);
        break;
    case 'barcode':
        if ($argc < 3) die("barcode <isbn>\n");
        $catalog->generateBarcode($argv[2]);
        break;
    default:
        echo "Unknown command\n";
}
?>

// book_catalog.js
#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const { program } = require('commander');
const bwipjs = require('bwip-js');

const DATA_FILE = 'books.json';
const BARCODE_DIR = 'barcodes';

class Book {
    constructor(title, author, isbn, year, publisher) {
        this.title = title;
        this.author = author;
        this.isbn = isbn;
        this.year = year;
        this.publisher = publisher;
    }
}

class Catalog {
    constructor() {
        this.books = [];
        this.load();
    }

    load() {
        if (fs.existsSync(DATA_FILE)) {
            const data = JSON.parse(fs.readFileSync(DATA_FILE));
            this.books = data.map(b => new Book(b.title, b.author, b.isbn, b.year, b.publisher));
        }
    }

    save() {
        fs.writeFileSync(DATA_FILE, JSON.stringify(this.books, null, 2));
    }

    add(title, author, isbn, year, publisher) {
        for (const b of this.books) {
            if (b.isbn === isbn) {
                console.log(`Book with ISBN ${isbn} already exists.`);
                return;
            }
        }
        const book = new Book(title, author, isbn, year, publisher);
        this.books.push(book);
        this.save();
        console.log(`✅ Added: "${title}" by ${author} (ISBN: ${isbn})`);
    }

    list() {
        if (this.books.length === 0) {
            console.log('No books in catalog.');
            return;
        }
        console.log('\n📋 All Books:');
        this.books.forEach((b, i) => {
            console.log(`${i+1}. ${b.title} (${b.isbn}) – ${b.author} (${b.year})`);
        });
    }

    search(term) {
        const lower = term.toLowerCase();
        const results = this.books.filter(b =>
            b.title.toLowerCase().includes(lower) ||
            b.author.toLowerCase().includes(lower) ||
            b.isbn.includes(term)
        );
        if (results.length === 0) {
            console.log('No matching books.');
            return;
        }
        console.log(`\n🔍 Search results for "${term}":`);
        results.forEach(b => {
            console.log(`${b.title} – ${b.author} (${b.isbn})`);
        });
    }

    show(isbn) {
        const book = this.books.find(b => b.isbn === isbn);
        if (!book) {
            console.log(`Book with ISBN ${isbn} not found.`);
            return;
        }
        console.log(`\n📖 Details for ${isbn}:`);
        console.log(`Title: ${book.title}`);
        console.log(`Author: ${book.author}`);
        console.log(`ISBN: ${book.isbn}`);
        console.log(`Year: ${book.year}`);
        console.log(`Publisher: ${book.publisher}`);
        const barcodePath = path.join(BARCODE_DIR, `${isbn}.png`);
        if (fs.existsSync(barcodePath)) {
            console.log(`Barcode: ${barcodePath}`);
        } else {
            console.log('Barcode not generated yet.');
        }
    }

    async generateBarcode(isbn) {
        if (!/^\d{13}$/.test(isbn)) {
            console.log('Invalid ISBN. Must be 13 digits.');
            return;
        }
        const book = this.books.find(b => b.isbn === isbn);
        if (!book) {
            console.log(`Book with ISBN ${isbn} not found.`);
            return;
        }
        try {
            fs.mkdirSync(BARCODE_DIR, { recursive: true });
            const png = await bwipjs.toBuffer({
                bcid: 'ean13',
                text: isbn,
                scale: 3,
                height: 10,
                includetext: true,
            });
            const filepath = path.join(BARCODE_DIR, `${isbn}.png`);
            fs.writeFileSync(filepath, png);
            console.log(`✅ Barcode generated: ${filepath}`);
        } catch (err) {
            console.error('Error generating barcode:', err);
        }
    }
}

program
    .command('add <title> <author> <isbn> <year> <publisher>')
    .action((title, author, isbn, year, publisher) => {
        const catalog = new Catalog();
        catalog.add(title, author, isbn, parseInt(year), publisher);
    });

program
    .command('list')
    .action(() => {
        const catalog = new Catalog();
        catalog.list();
    });

program
    .command('search <term>')
    .action((term) => {
        const catalog = new Catalog();
        catalog.search(term);
    });

program
    .command('show <isbn>')
    .action((isbn) => {
        const catalog = new Catalog();
        catalog.show(isbn);
    });

program
    .command('barcode <isbn>')
    .action(async (isbn) => {
        const catalog = new Catalog();
        await catalog.generateBarcode(isbn);
    });

program.parse(process.argv);

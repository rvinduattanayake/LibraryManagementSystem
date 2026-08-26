# Library Management System

ASP.NET Core MVC web application implementing a library management system with six core processes:

| Process | Description |
|---------|-------------|
| **Book Registration** | Librarian registers titles and up to 10 copies per book number |
| **User Registration** | Register borrowers (user number, name, sex, national ID, address) |
| **Loan** | Borrow up to 5 books for 14 days; reference copies cannot be borrowed |
| **Return** | Accept returns; notify oldest reserver when applicable |
| **Reservation** | FIFO queue when no copies are available |
| **Inquiry** | Search by accession number, title, or author |

## Domain Model

- **BookTitle** — class of identical books (e.g. same ISBN/edition). Has a book number.
- **BookCopy** — physical copy on the shelf. Accession number = book number + copy suffix.
- **Borrower** — registered library member.
- **Loan** — links borrower to copy with pending/active/returned status.
- **Reservation** — FIFO queue per title.

### Numbering

- Book number: `{Classification}-{Sequence}` → e.g. `005.5-0001`
- Accession number: `{BookNumber}-{CopyNumber}` → e.g. `005.5-0001-01`

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)

## Run

```bash
cd LibraryManagementSystem
dotnet restore
dotnet run
```

Open http://localhost:5000 in your browser.

Data is stored in `library.db` (SQLite) in the project folder.

## Suggested Workflow

1. **Register books** — Books → Register Book
2. **Register borrowers** — Users → Register User
3. **Inquire availability** — Inquiry → Search
4. **Loan books** — Loans → submit request → librarian Confirm
5. **Return books** — Returns → enter accession number
6. **Reserve unavailable titles** — Reservations → place reservation

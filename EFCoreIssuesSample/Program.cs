using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFCoreIssuesSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var ctx = new BlogContext();
            //ctx.Database.EnsureDeleted();
            //ctx.Database.EnsureCreated();

            //works
            var result = await ctx.Book.Where(x => EF.Functions.Like(x.Title, "Two State")).ToListAsync();
            
            //fails
            result = await ctx.Book.Where(x => x.Title.Value.Contains("Two State")).ToListAsync();
        }
    }

    public class BlogContext : DbContext
    {
        public DbSet<Book> Book { get; set; }

        static ILoggerFactory ContextLoggerFactory
        => LoggerFactory.Create(b => b.AddConsole().AddFilter("", LogLevel.Information));

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseSqlServer("Server=.\\;Database=EnterpriseArchitecture;Trusted_Connection=true;")
                .EnableSensitiveDataLogging()
                .UseLoggerFactory(ContextLoggerFactory);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>(entity =>
            {
                entity.HasKey(e => new { e.Id });

                entity.Property(p => p.Title)
                   .IsRequired()
                   .HasMaxLength(100)
                   .HasConversion(p => p.Value, p => Title.Create(p).Value);
            });
        }
    }

    public class Title : ValueObject
    {
        public string Value { get; }

        protected Title()
        {
        }

        private Title(string value) : this()
        {
            Value = value;
        }

        public static Result<Title> Create(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Result.Failure<Title>("Title should not be empty");

            title = title.Trim();

            if (title.Length > 100)
                return Result.Failure<Title>("Title is too long");

            return Result.Success(new Title(title));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public static implicit operator string(Title title)
        {
            return title.Value;
        }
    }

    public class Book
    {
        protected Book()
        {
        }

        public Book(Title title) : this()
        {
            Title = title;
        }

        public int Id { get; set; }
        public Title Title { get; private set; }
    }
}

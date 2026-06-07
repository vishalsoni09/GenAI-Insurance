using Microsoft.EntityFrameworkCore;
using GenAI_Insurance.Server.Models;

namespace GenAI_Insurance.Server.Data;

public class LoanDbContext : DbContext
{
    public LoanDbContext(DbContextOptions<LoanDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;
    // Add other DbSets as needed, e.g. Loans, Policies
}

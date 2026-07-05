using LovePlaces.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LovePlaces.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Place> Places => Set<Place>();
}
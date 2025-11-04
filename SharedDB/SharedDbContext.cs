using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharedDB
{
    public class SharedDbContext : DbContext
    {
        public DbSet<TokenDb> Tokens { get; set; }
        public DbSet<ServerDb> Servers { get; set; }


        public SharedDbContext()
        {
            
        }

        public SharedDbContext(DbContextOptions<SharedDbContext> options) : base(options)
        {

        }

        public static string ConnectionString { get; set; } = @"Data Source=(localdb)\ProjectModels;Initial Catalog=SharedDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";


        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (options.IsConfigured == false)
            {
                options                    
                    .UseSqlServer(ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<TokenDb>()
                .HasIndex(a => a.AccountDbId)
                .IsUnique();

            builder.Entity<ServerDb>()
                .HasIndex(a => a.Name)
                .IsUnique();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace microservcolegio.Secretaria.Entities;
public class RepositoryDbContext : DbContext
{
    public DbSet<Aluno> Alunos {get;set;}
    private IConfiguration _configuration;
    public RepositoryDbContext(IConfiguration configuration){
        this._configuration = configuration;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseCosmos(
            connectionString: this._configuration["CosmosDBURL"],
            databaseName: this._configuration["CosmosDBDBName"],
            cosmosOptionsAction: options =>
            {
                options.ConnectionMode(ConnectionMode.Gateway);
                options.HttpClientFactory(() => new HttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }));
            }

        );
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder){
        modelBuilder.Entity<Aluno>()
            .HasNoDiscriminator();
        modelBuilder.Entity<Aluno>()
            .ToContainer("aluno");
        modelBuilder.Entity<Aluno>()
            .Property(p=>p.id)
            .HasValueGenerator<GuidValueGenerator>();
        modelBuilder.Entity<Aluno>()
            .HasPartitionKey(p => p.id);
    }

}

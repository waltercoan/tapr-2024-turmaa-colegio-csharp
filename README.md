# tapr-2024-turmaa-colegio-csharp

## Documentação do projeto
[Diagramas](https://univillebr-my.sharepoint.com/:u:/g/personal/walter_s_univille_br/EbLNg-hQDmdIjM6sIIFvjA0BHpsa_cRHPT0BpNIaea0yXw?e=tPsYS0)

## Extensões do VSCode
[C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit?wt.mc_id=AZ-MVP-5003638)

[C# Extension](https://marketplace.visualstudio.com/items?itemName=kreativ-software.csharpextensions?wt.mc_id=AZ-MVP-5003638)

[Rest Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client?wt.mc_id=AZ-MVP-5003638)

## Criação do projeto
```
dotnet net webapi -o microserv<nome do seu subdominio>
dotnet dev-certs https --trust
```
1. Criar um namespace com o nome de cada Bounded Context
2. Criar um namespace chamado Entities e dentro dele criar as entidades
```
├── Secretaria
│   └── Entities
│       └── Aluno.cs
```

## Cosmos DB
[Introdução (https://learn.microsoft.com/en-us/azure/cosmos-db/introduction?wt.mc_id=AZ-MVP-5003638)](https://learn.microsoft.com/en-us/azure/cosmos-db/introduction?wt.mc_id=AZ-MVP-5003638)

[Databases, Containers e Itens (https://learn.microsoft.com/en-us/azure/cosmos-db/resource-model?wt.mc_id=AZ-MVP-5003638)](https://learn.microsoft.com/en-us/azure/cosmos-db/resource-model?wt.mc_id=AZ-MVP-5003638)

```
docker run \
    --publish 8081:8081 \
    --publish 10250-10255:10250-10255 \
    --name cosmosdb-linux-emulator \
    --detach \
    mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest    
```
### Instalação do certificado
```
curl --insecure https://localhost:8081/_explorer/emulator.pem > ~/emulatorcert.crt
```
```
sudo cp ~/emulatorcert.crt /usr/local/share/ca-certificates/
```
```
sudo update-ca-certificates
```
### IMPORTANTE: nas configurações do CodeSpace desabilitar a opção http.proxyStrictSSL

### Extensão do VSCode
[Azure Databases](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-cosmosdb?wt.mc_id=AZ-MVP-5003638)
### Endpoint do simulador
```
AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;
```

### Modelagem de dados
[Modeling Data](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/modeling-data?wt.mc_id=AZ-MVP-5003638)

### Particionamento
[Partitioning](https://learn.microsoft.com/en-us/azure/cosmos-db/partitioning-overview?wt.mc_id=AZ-MVP-5003638)

### Instalar as bibliotecas
```
    cd microservcolegio/
    dotnet add package Azure.Identity
    dotnet add package Microsoft.EntityFrameworkCore.Cosmos
```
- Criar o arquivo .env na raiz do projeto para setar o ambiente de desenvolvimento

```
ASPNETCORE_ENVIRONMENT=Development
```
## Importante: correção da classe RepositoryDBContext para conectar no simulador do ComosDB
```
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace microservcolegio.Secretaria.Entities;

public class RepositoryDbContext : DbContext
{
    private IConfiguration _configuration;
    public DbSet<Aluno> Alunos {get;set;}

    public RepositoryDbContext(IConfiguration configuration){
        this._configuration = configuration;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Aluno>()
            .ToContainer("aluno")
            .HasPartitionKey(p => p.id)
            .HasAutoscaleThroughput(400)
            .HasNoDiscriminator()
            .Property(p => p.id)
            .HasValueGenerator<GuidValueGenerator>()
            .IsRequired(true)
            ;


        

    }

}
```
## CRUD API REST
### Verbo GET e POST
- Objetivo: Retornar uma lista de objetos ou um objeto específico a partir da chave

#### IAlunoService.cs
- Criar os métodos na interface do serviço

```
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using microservcolegio.Secretaria.Entities;

namespace microservcolegio.Secretaria.Services;

public interface IAlunoService
{
    Task<List<Aluno>> GetAllAsync();
    Task<Aluno> SaveAsync(Aluno aluno);
}
```

#### AlunoService.cs
- Implementar a lógica de consulta na classe concreta do serviço

```
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using microservcolegio.Secretaria.Entities;
using Microsoft.EntityFrameworkCore;

namespace microservcolegio.Secretaria.Services;
public class AlunoService : IAlunoService
{
    private RepositoryDbContext _dbContext;
    public AlunoService(RepositoryDbContext dbContext){
        this._dbContext = dbContext;
    }
    
    public async Task<List<Aluno>> GetAllAsync()
    {
        var listaAlunos = await _dbContext.Alunos.ToListAsync();
        return listaAlunos;
    }

    public async Task<Aluno> SaveAsync(Aluno aluno)
    {
        await this._dbContext.AddAsync(aluno);
        await this._dbContext.SaveChangesAsync();
        
        return aluno;
    }
}
```

#### AlunoController.cs
```
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using microservcolegio.Secretaria.Entities;
using microservcolegio.Secretaria.Services;
using Microsoft.AspNetCore.Mvc;

namespace microservcolegio.Secretaria.Controllers;
[ApiController]
[Route("/api/v1/[controller]")]
public class AlunoController : ControllerBase
{
    private IAlunoService _service;
    public AlunoController(IAlunoService service){
        this._service = service;
    }
    [HttpGet]
    public async Task<IResult> Get(){
        var listaAlunos = await this._service.GetAllAsync();
        return Results.Ok(listaAlunos);
    }
    [HttpPost]
    public async Task<IResult> Post(Aluno aluno){
        if(aluno == null){
            return Results.BadRequest();
        }

        var alunoSalvo = await _service.SaveAsync(aluno);

        return Results.Ok(alunoSalvo);
    }
}
```

### Arquivo Program.cs
```
using microservcolegio.Secretaria.Entities;
using microservcolegio.Secretaria.Services;

var builder = WebApplication.CreateBuilder(args);

//IMPORTANTE
builder.Services.AddControllers();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<RepositoryDbContext>();
builder.Services.AddScoped<IAlunoService,AlunoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
//IMPORTANTE!!!
app.MapControllers();

app.Run();
```
#### teste.rest
- Implementação do teste do verbo GET e POST

```
### Buscar todos os alunos
GET http://localhost:5011/api/v1/aluno

### Inserir um aluno
POST http://localhost:5011/api/v1/aluno
Content-Type: application/json

{
    "nome" : "zezinho"
}

```
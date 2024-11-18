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
# Azure Service Bus
- [Documentação](https://azure.microsoft.com/pt-br/products/service-bus)
- Passo 1: Criar uma instância do recurso Service Bus, informando o namespace name e o pricing tier Standard (a partir desse SKU há suporte a tópicos)
![servicebus001](diagramas/servicebus001.png "servicebus001")
- Passo 2: Uma vez provisionado, clicar no menu tópicos
![servicebus002](diagramas/servicebus002.png "servicebus002")
- Passo 3: Clicar no link para criar um novo tópico
![servicebus003](diagramas/servicebus003.png "servicebus003")
- Passo 4: Informar o nome do tópico no padrão topico-NOMEDOMICROSERVICO-NOMEDAENTIDADE
![servicebus004](diagramas/servicebus004.png "servicebus004")
- Passo 5: Uma vez que o tópico seja provisionado, clicar em subscriptions
![servicebus005](diagramas/servicebus005.png "servicebus005")
- Passo 6: Clicar no link para criar uma nova subscription
![servicebus006](diagramas/servicebus006.png "servicebus006")
- Passo 7: Informar o nome da assinatura no padrão subs-topico-NOMEDOMICROSERVICO-NOMEDAENTIDADE
![servicebus007](diagramas/servicebus007.png "servicebus007")
- Passo 8: Clicar no ícone Service Bus Explorer para monitorar as mensagens
![servicebus008](diagramas/servicebus008.png "servicebus008")


# Dapr
- Dapr é um runtime para construção, integração, execução e monitoramento de aplicações distribuídas no formato de microsserviços
![Dapr](https://docs.dapr.io/images/overview.png "Dapr")
- [Building blocks](https://docs.dapr.io/concepts/overview/#microservice-building-blocks-for-cloud-and-edge)

## Instalação
- [Instalação do Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/)

## Inicialização
```
dapr init
```

- Verificando a instalação
```
$ docker ps
CONTAINER ID   IMAGE                COMMAND                  CREATED          STATUS                    PORTS                                                                                                                                     NAMES
f377a492bae6   daprio/dapr:1.12.1   "./placement"            43 seconds ago   Up 42 seconds             0.0.0.0:50005->50005/tcp, :::50005->50005/tcp, 0.0.0.0:58080->8080/tcp, :::58080->8080/tcp, 0.0.0.0:59090->9090/tcp, :::59090->9090/tcp   dapr_placement
a5009c20daa7   redis:6              "docker-entrypoint.s…"   47 seconds ago   Up 44 seconds             0.0.0.0:6379->6379/tcp, :::6379->6379/tcp                                                                                                 dapr_redis
1d669098ac80   openzipkin/zipkin    "start-zipkin"           48 seconds ago   Up 44 seconds (healthy)   9410/tcp, 0.0.0.0:9411->9411/tcp, :::9411->9411/tcp                                                                                       dapr_zipkin
```

## Dependências no POM
- [SDK .NET](https://docs.dapr.io/developing-applications/sdks/dotnet/)
```
dotnet add package Dapr.Client
dotnet add package Dapr.AspNetCore
```
## Componentes Dapr
- Os componentes do Dapr são recursos utilizados pelos microsserviços que são acessados através do sidecar.
- [Dapr Components](https://docs.dapr.io/reference/components-reference/)
- Passo 1: criar uma pasta components
- Passo 2: na pasta components criar o arquivo servicebus-pubsub.yaml

```
# Documentação: https://docs.dapr.io/reference/components-reference/supported-pubsub/setup-azure-servicebus/
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: servicebus-pubsub
spec:
  type: pubsub.azure.servicebus.topics
  version: v1
  metadata:
  - name: namespaceName
    value: "tapr2023.servicebus.windows.net"
```

- Passo 3.1: na pasta do projeto executar o comando.

```
dotnet build --configuration Release
```

- Passo 3.2: na pasta principal do projeto (mesma pasta do arquivo appsettings.json), criar um novo arquivo com o nome dapr.yaml
```
version: 1
common:
  resourcesPath: ./components/
apps:
  - appID: tapr-2024-NOMEDOMICROSERVICO-dotnet
    appDirPath: .
    appPort: 5202
    command: ["dotnet", "run", "--configuration","Development"]
```

## Publicação de atualizações da entidade principal do agregado

- Passo 4: alterar o arquivo appsettings.json para incluir dois novos parametros:
  - "AppComponentTopicCarro":"<nome do tópico registrado no service bus>",
  - "AppComponentService":"servicebus-pubsub"

```
#Exemplo
  "AppComponentTopicCarro":"topico-NOMEDOMICROSERVICO-NOMEDAENTIDADE",
  "AppComponentService":"servicebus-pubsub"
```

- Passo 5: na classe de serviço da entidade root do agregado, incluir os seguintes códigos:

```
//outros usings...
using Dapr.Client;

public class CarroService : ICarroService
{
    //outros atributos...
    private IConfiguration _configuration;
    private DaprClient _daprClient;
    public CarroService(RepositoryDbContext dbContext,
                        IConfiguration configuration)
    {
        this._dbContext = dbContext;
        this._configuration = configuration;
        this._daprClient = new DaprClientBuilder().Build();
        
    }
    //Método para publicar o novo evento
    private async Task PublishUpdateAsync(Carro carro){
        await this._daprClient.PublishEventAsync(_configuration["AppComponentService"], 
                                                _configuration["AppComponentTopicCarro"], 
                                                carro);
    }
    
    public async Task<Carro> saveNewAsync(Carro carro)
    {
        carro.id = Guid.Empty;
        await _dbContext.Carros.AddAsync(carro);
        await _dbContext.SaveChangesAsync();
        //chamar o método para publicar o evento
        await PublishUpdateAsync(carro);
        return carro;
    }

    public async Task<Carro> updateAsync(string id, Carro carro)
    {
        var carroAntigo = await _dbContext.Carros.Where(c => c.id.Equals(new Guid(id))).FirstOrDefaultAsync();        
        if (carroAntigo != null){
            //Atualizar cada atributo do objeto antigo 
            carroAntigo.modelo = carro.modelo;
            await _dbContext.SaveChangesAsync();
            //chamar o método para publicar o evento
            await PublishUpdateAsync(carroAntigo);
        }
        return carroAntigo;
    }
```

## Executar o teste de publicação de eventos
```
#Executar esse comando dentro da pasta do projeto
dotnet build --configuration Release
dapr run -f .
```
- Passo 6: Usar o arquivo teste.rest para invocar a API REST nos métodos POST e PUT, verificar no Azure Service Bus se os eventos foram publicados no tópico.

## Assinatura das atualizações em um tópico
- Escolher uma das entidades externas aos agregados.

- Passo 1: Criar na classe Controller da entidade externa ao agregado um novo end point chamado Update, que será automaticamente chamado pelo Dapr toda vez que um novo evento for publicado no Service Bus

```
    [Topic(pubsubName:"servicebus-pubsub",name:"topico-equipe-0-cliente")] 
    [HttpPost("/event")]
    public async Task<IResult> UpdateClient(Cliente Cliente){      
        if(Cliente == null){
            return Results.BadRequest();
        }
        Console.WriteLine("EVENT" + Cliente.Nome);
        await _service.updateEventAsync(Cliente);

        return Results.Ok(Cliente);
    }
```
- Passo 3: alterar a classe de serviço da entidade, para incluir um método update recebendo como parâmetro apenas a classe de entidade.

```
public interface IClienteService
{
    Task<List<Cliente>> GetAllAsync();
    Task<Cliente> GetByIdAsync(string id);
    Task<Cliente> saveNewAsync(Cliente cliente);
    Task<Cliente> updateAsync(String id, Cliente cliente);
    Task<Cliente> DeleteAsync(String id);
    Task<Cliente> updateEventAsync(Cliente cliente);
}
```
- Passo 4: incluir na classe de implementação do serviço da entidade, o código do método abaixo para receber a entidade e atualizar no banco de dados local do serviço.

```
    public async Task<Cliente> updateEventAsync(Cliente cliente)
    {
        var clienteAntigo = await _dbContext.Clientes.Where(c => c.id.Equals(cliente.id)).FirstOrDefaultAsync();
        if (clienteAntigo == null){
            await _dbContext.Clientes.AddAsync(cliente);
            await _dbContext.SaveChangesAsync();
        }else{
            await updateAsync(cliente.id.ToString(),cliente);
        }
        return cliente;
    }
```
## Executar o teste de assinatura dos eventos
```
#Executar esse comando dentro da pasta do projeto
dapr run -f .
```
- Mantendo a aplicação em execução, abrir um novo terminal e executar o exemplo do comando abaixo alterando os parametros para simular a publicação de um evento.

```
#Exemplo de publicação de atualização do evento
# dapr publish --publish-app-id <nome da aplicação no arquivo dapr.yaml> --pubsub <nome do componente do service bus no arquivo /componenets/servicebus-pubsub.yaml> --topic <nome do topico registrado no service bus> --data '<objeto JSON contendo os campos da entidade>'

dapr publish --publish-app-id tapr-2024-NOMEDOMICROSERVICO-dotnet --pubsub servicebus-pubsub --topic topico-NOMEDOMICROSERVICO-NOMEDAENTIDADE --data '{"id": "536b15ee-e52f-4f06-b22d-51dfe2d18d79","nome": "Zezinho","endereco": "Rua lalala 100"}'
```

- Verificar no banco de dados se a entidade

- IMPORTANTE: caso o número de mensagens na fila de mensagens mortas (Dead-Letter queue), é porque a mensagen enviada no passo anterior tem algum erro de formatação em relação a entidade da aplicação em .net

using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

//var patientDataHistoryDb =
//    builder.AddConnectionString("patientDataHistoryDb", "Server=localhost;Database=aspire;User Id=sa;Password=Password");


var redisDb = builder.AddRedis("cacheDb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRedisInsight()
    .WithLifetime(ContainerLifetime.Persistent);

var rabbit = builder.AddRabbitMQ("rabbitMQ")
    .WithLifetime(ContainerLifetime.Persistent);

var sqlServer = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent);
    

var patientDataHistoryDb = sqlServer.AddDatabase("patientDataHistoryDb");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("postgres-v")
    .WithLifetime(ContainerLifetime.Persistent);

var patientDataDb = postgres.AddDatabase("patientDataDb");

var bff = builder.AddProject<Projects.WebClientBffGateway>("webclientbffgateway");


var patientDataApi = builder.AddProject<Projects.PatientDataAPI>("patientdataapi")
    .WithReference(rabbit).WaitFor(rabbit)
    .WithReference(patientDataDb)
    .WaitFor(patientDataDb);


var alerting = builder.AddProject<Projects.AlertingService>("alertingservice")
    .WithReference(rabbit)
    .WaitFor(rabbit);

var history = builder.AddProject<Projects.PatientHistoryService>("patienthistoryservice")
    .WithReference(rabbit)
    .WithReference(patientDataHistoryDb)
    .WaitFor(patientDataHistoryDb)
    .WaitFor(rabbit);

var monitoring = builder.AddProject<Projects.PatientMonitoringService>("patientmonitoringservice")
    .WithReference(redisDb).WaitFor(redisDb)
    .WithReference(rabbit).WaitFor(rabbit);

// Add Ollama LLM container for local AI inference
//var ollama = builder.AddContainer("ollama", "ollama/ollama:latest")
//    .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "ollama-http")
//    .WithBindMount("ollama_data", "/root/.ollama")
//    .WithEnvironment("OLLAMA_MODELS", "llama2") // You can change model as needed
//    .WithLifetime(ContainerLifetime.Persistent);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithContainerRuntimeArgs("--gpus=all")
    .WithOpenWebUI(); 

var phi35 = ollama.AddModel("llama3");

// Add Qdrant vector database container for RAG integration
var qdrant = builder.AddContainer("qdrant", "qdrant/qdrant:latest")
    .WithHttpEndpoint(port: 6333, targetPort: 6333, name: "qdrant-http")
    .WithLifetime(ContainerLifetime.Persistent);

// Add RagService project
var ragService = builder.AddProject<Projects.RagService>("ragservice")
    .WaitFor(qdrant);

// Add ChatService project
var chatService =  builder.AddProject<Projects.ChatService>("chatservice")
    .WithReference(phi35)
    .WaitFor(ollama);

bff
.WithReference(patientDataApi)
.WithReference(monitoring)
.WithReference(history)
.WithReference(alerting)
.WithReference(chatService) // Add chatService as a reference
.WaitFor(patientDataApi)
.WaitFor(monitoring)
.WaitFor(history)
.WaitFor(alerting)
.WaitFor(chatService);


var angular = builder.AddNpmApp("angular", "../Clients/patient-monitoring-client")
    .WithReference(bff)
    .WaitFor(bff)
    .PublishAsDockerFile()
    .WithHttpEndpoint(env: "PORT");

//.WithReference("addiia", new Uri("https://jsonplaceholder.typicode.com"))


//.WithReference("addiia", new Uri("https://jsonplaceholder.typicode.com"))

builder.Build().Run();

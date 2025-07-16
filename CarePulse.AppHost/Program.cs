using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var ollama = builder.AddOllama("ollama")
    .WithImage("ollama/ollama:0.9.6")
    .WithGPUSupport()
    .WithDataVolume()
    .WithOpenWebUI();

var llama3 = ollama.AddModel("phi3:mini");
var embed = ollama.AddModel("nomic-embed-text");

//var openAi = builder.AddOpenAI("my-openai", secret: "OpenAI__ApiKey");
var openAIResource = builder.AddConnectionString("openai");

//builder.addOpenAI


var qdrant = builder.AddContainer("qdrant", "qdrant/qdrant:latest")
    .WithHttpEndpoint(port: 9123, targetPort: 6333, name: "qdrant-http")
     .WithEndpoint(port: 8992, targetPort: 6334, name: "qdrant-grpc")
     
      .WithLifetime(ContainerLifetime.Persistent);

var endpoint = qdrant.GetEndpoint("qdrant-grpc");


// Add RagService project
var ragService = builder.AddProject<Projects.RagService>("ragservice")
    .WaitFor(qdrant)
    .WithReference(endpoint);



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
    .WithReference(embed)
    .WithReference(ragService)
    .WithReference(rabbit).WaitFor(rabbit);

// Add Ollama LLM container for local AI inference
//var ollama = builder.AddContainer("ollama", "ollama/ollama:latest")
//    .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "ollama-http")
//    .WithBindMount("ollama_data", "/root/.ollama")
//    .WithEnvironment("OLLAMA_MODELS", "llama2") // You can change model as needed
//    .WithLifetime(ContainerLifetime.Persistent);


// Add Qdrant vector database container for RAG integration
// Add ChatService project
var chatService = builder.AddProject<Projects.ChatService>("chatservice")
    .WithReference(llama3)
    .WithReference(openAIResource)
    .WithReference(monitoring)
    .WithReference(history)
    .WithReference(embed).WithReference(ragService)
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
    .WithHttpEndpoint(env: "PORT", port: 4200)
    .WithReference(bff)
    .WaitFor(bff)
    .WithExternalHttpEndpoints();



builder.Build().Run();

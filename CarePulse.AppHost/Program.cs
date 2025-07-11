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


bff
.WithReference(patientDataApi)
.WithReference(monitoring)
.WithReference(history)
.WithReference(alerting)
.WaitFor(patientDataApi)
.WaitFor(monitoring)
.WaitFor(history)
.WaitFor(alerting);



var angular = builder.AddNpmApp("angular", "../Clients/patient-monitoring-client")
    .WithReference(bff)
    .WaitFor(bff)
    .PublishAsDockerFile()
    .WithHttpEndpoint(env: "PORT");

//.WithReference("addiia", new Uri("https://jsonplaceholder.typicode.com"))

builder.Build().Run();

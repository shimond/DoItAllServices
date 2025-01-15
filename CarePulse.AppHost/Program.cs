var builder = DistributedApplication.CreateBuilder(args);

var redisDb = builder.AddRedis("cacheDb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRedisInsight().WithLifetime(ContainerLifetime.Persistent);

var rabbit = builder.AddRabbitMQ("rabbitMQ")
    .WithLifetime(ContainerLifetime.Persistent);

var patientDataHistoryDb = builder.AddSqlServer("patinetDataHistoryDb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var patientDataDb = builder.AddPostgres("patientDataDb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);


var bff = builder.AddProject<Projects.WebClientBffGateway>("webclientbffgateway");

var angular = builder.AddNpmApp("angular", "../Clients/patient-monitoring-client")
    .WithReference(bff)
    .WaitFor(bff)
    .PublishAsDockerFile()

    .WithHttpEndpoint(env: "PORT");

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

//.WithReference("addiia", new Uri("https://jsonplaceholder.typicode.com"))

builder.Build().Run();

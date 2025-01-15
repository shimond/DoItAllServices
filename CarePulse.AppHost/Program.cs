var builder = DistributedApplication.CreateBuilder(args);

var redisDb = builder.AddRedis("cacheDb")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRedisInsight();
var rabbit = builder.AddRabbitMQ("rabbitMQ")
    .WithLifetime(ContainerLifetime.Persistent);
var patientDataHistoryDb = builder.AddSqlServer("patinetDataHistoryDb")
    .WithLifetime(ContainerLifetime.Persistent);
var patientDataDb = builder.AddSqlServer("patientDataDb")
    .WithLifetime(ContainerLifetime.Persistent);


var bff = builder.AddProject<Projects.WebClientBffGateway>("webclientbffgateway");

builder.AddNpmApp("angular", "../Clients/patient-monitoring-client")
    .WithReference(bff)
    .WaitFor(bff)
    .WithHttpEndpoint(env: "PORT");

var patientData = builder.AddProject<Projects.PatientDataAPI>("patientdataapi")
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
.WithReference(patientData)
.WithReference(monitoring)
.WithReference(history)
.WithReference(alerting);

builder.Build().Run();

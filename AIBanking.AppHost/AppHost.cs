var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddConnectionString("DefaultConnection");

var api   = builder.AddProject<Projects.AIBanking>("aibanking")
                   .WithReference(db);
var agent = builder.AddProject<Projects.AIBanking_AIAgent>("aiagent")
                   .WithReference(db)
                   .WithReference(api);

builder.AddNpmApp("web", "../AIBanking.Web", "dev")
       .WithReference(api)
       .WithReference(agent)
       .WithHttpEndpoint(port: 5173, env: "PORT")
       .WithExternalHttpEndpoints();

builder.Build().Run();

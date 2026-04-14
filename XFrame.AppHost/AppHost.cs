var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.XFrame>("xframe");

builder.Build().Run();
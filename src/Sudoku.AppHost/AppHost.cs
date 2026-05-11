var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var database = postgres.AddDatabase("sudokudb");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator.WithDataVolume());
var profilePictures = storage.AddBlobContainer("profile-pictures", "profile-pictures");

var api = builder.AddProject<Projects.Sudoku_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(profilePictures, "profile-pictures")
    .WaitFor(profilePictures)
    .WithEnvironment("E2E__UseDeterministicGenerator", builder.Configuration["E2E:UseDeterministicGenerator"] ?? "false");

var web = builder.AddViteApp("web", "../Sudoku.Web")
    .WithNpm()
    .WithReference(api)
    .WithEnvironment("SUDOKU_API_PROXY_TARGET", api.GetEndpoint("https"))
    .WaitFor(api);

builder.Build().Run();

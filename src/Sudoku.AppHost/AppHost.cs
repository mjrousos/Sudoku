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
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"))
    .WaitFor(api);

api.WithEnvironment("Cors__AllowedOrigins__0", web.GetEndpoint("http"));

builder.Build().Run();

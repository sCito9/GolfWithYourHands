using MongoDB.Driver;
using SGServer.Middleware;

var builder = WebApplication.CreateBuilder(args);

// MongoDB Settings
var mdbSettings = builder.Configuration.GetSection("MongoDbSettings");
var mdbConnectionString = mdbSettings["ConnectionString"];
var mdbDatabaseName = mdbSettings["DatabaseName"];

// Settings
var requireApiKey = builder.Configuration.GetValue<bool>("RequireApiKey");

// Change JSON Options
builder.Services.AddControllers()
    .AddJsonOptions(options => 
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Configure MongoDB
builder.Services.Configure<SGServer.Models.MongoDbSettings>(mdbSettings);
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mdbConnectionString));
builder.Services.AddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mdbDatabaseName));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/Error");


// Apply API Key Authentication
if (requireApiKey)
    app.UseApiKeyAuthentication();

app.UseRouting();

app.MapControllers();

// Initialize the ID counters
var mongoClient = app.Services.GetRequiredService<IMongoClient>();
SGServer.Models.User.InitializeIdCounter(mongoClient, mdbDatabaseName ?? "SGDatabase");
SGServer.Models.Club.InitializeIdCounter(mongoClient, mdbDatabaseName ?? "SGDatabase");

app.Run();

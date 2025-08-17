using DotnetGeminiSDK;
using SnippetMaster.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
string value;
if (builder.Environment.IsDevelopment())
{
    value = builder.Configuration["API_KEY_GEMINI"]!;
    Environment.SetEnvironmentVariable("API_KEY_GEMINI", value);
}
else
{
    value = Environment.GetEnvironmentVariable("API_KEY_GEMINI")!.ToString();
}
builder.Services.AddGeminiClient(config =>
{
    config.ApiKey = $"{value}";
});
builder.Services.AddTransient<ITextFormatterService, TextFormatterService>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using playwright.test.generator;
using playwright.test.generator.Abstractions;
using playwright.test.generator.IocConventions;
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.RegisterByConvention<PlayWrightTestGenerator>();


var app = builder.Build();

var playWrightTestGenerator = app.Services.GetRequiredService<IPlayWrightTestGenerator>();
var res = await playWrightTestGenerator.GenerateTest(new GenerateTestRequest
{

});

Console.WriteLine(JsonSerializer.Serialize(res));  



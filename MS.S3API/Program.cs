using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Configure AWS options
var awsOptions = builder.Configuration.GetAWSOptions();
awsOptions.DefaultClientConfig.ServiceURL = builder.Configuration["AWS:ServiceURL"];
awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(
    builder.Configuration["AWS:AccessKey"],
    builder.Configuration["AWS:SecretKey"]
);
builder.Services.AddAWSService<IAmazonS3>(awsOptions);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

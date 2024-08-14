using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using AzureDevOpsPolicyGuard.Application.Services;
using AzureDevOpsPolicyGuard.Infrastructure.Repositories;
using AzureDevOpsPolicyGuard.Support;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Services.Common;

var builder = WebApplication.CreateBuilder(args);

var uri = builder.Configuration["KeyVault:Uri"];
var secretName = builder.Configuration["KeyVault:CertSecretName"];
var vaultService = new SecretClient(new Uri(uri), new DefaultAzureCredential());
var secret = await vaultService.GetSecretAsync(secretName);

builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer((o) =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret.Value.Value)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateActor = false,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidateLifetime = true
        };    
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

builder.Services.AddSingleton<IAzureDevopsService, AzureDevopsService>();
builder.Services.AddSingleton<IOrganizationCacheService, OrganizationCacheService>();
builder.Services.AddSingleton<IRepoNoDeleteRepository, RepoNoDeleteRepository>();


var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(configure => configure
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Movies.Api.Auth;
using Movies.Api.Endpoints;
using Movies.Api.Health;
using Movies.Api.Mapping;
using Movies.Api.Swagger;
using Movies.Application;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddAuthentication(scheme =>
{
    scheme.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    scheme.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    scheme.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(scheme =>
{
    scheme.TokenValidationParameters = new TokenValidationParameters
    {
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        ValidateIssuer = true,
        ValidateAudience = true
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthConstants.AdminUserPolicyName, p => p.AddRequirements(new AdminAuthRequirement(configuration["ApiKey"]!)));
    options.AddPolicy(AuthConstants.TrustedMemberPolicyName, p => p.RequireAssertion(c =>
        c.User.HasClaim(m => m is { Type: AuthConstants.AdminUserClaimName, Value: "true" }) ||
        c.User.HasClaim(m => m is { Type: AuthConstants.TrustedMemberClaimName, Value: "true" })));
});

builder.Services.AddScoped<ApiKeyAuthFilter>();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1.0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new MediaTypeApiVersionReader("api-version");
})
    .AddApiExplorer();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(options => options.OperationFilter<SwaggerDefaultValues>());

builder.Services.AddOutputCache(config =>
{
    config.AddBasePolicy(p => p.Cache());
    config.AddPolicy("MoviePolicy", p => 
        p.Cache()
            .Expire(TimeSpan.FromMinutes(1))
            .SetVaryByQuery(new []{"title", "year", "sortBy", "page", "pageSize"})
            .Tag("movies")
        );
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.HealthCheckName);

builder.Services.AddApplication();
builder.Services.AddDatabase(configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName);
        }
    });
}

app.MapHealthChecks("_health");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.UseMiddleware<ValidationMappingMiddleware>();
app.MapApiEndpoints();

app.Run();
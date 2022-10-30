

using Amazon.DynamoDBv2;
using MRB.DistributedLock;
using MRB.DistributedLock.Repository;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO.Pipelines;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddDynamoDistributedLock("distributed-lock");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.MapPost("/lock", async () =>
{
    using (var scope = app.Services.CreateScope())
    {

        //The first time you call this it can be slow, aws is configuring and loading
        //The AWS class is a singleton so you only pay this price once
        var lockClient = scope.ServiceProvider.GetRequiredService<ILockClient>();
        SampleEntity entity = new SampleEntity();

        //Grab a lock, it will release when the block 
        using (var token = await lockClient.TryAcquireLockAsync(typeof(SampleEntity), entity.Id, CancellationToken.None))
        {
            Debug.Assert(token != null);

            //We got the lock do some work
            Thread.Sleep(500);
        }
    }
}).WithName("lock");

app.MapPost("/denylock", async () =>
{
    using (var scope = app.Services.CreateScope())
    {
        var lockClient = scope.ServiceProvider.GetRequiredService<ILockClient>();
        SampleEntity entity = new SampleEntity();

        //grab a lock
        using (var token = await lockClient.TryAcquireLockAsync(typeof(SampleEntity), entity.Id, CancellationToken.None))
        {
            Debug.Assert(token != null);

            //try to grab it again, this should return null since we already locked it above
            using (var token2 = await lockClient.TryAcquireLockAsync(typeof(SampleEntity), entity.Id, CancellationToken.None))
            {
                Debug.Assert(token2 == null);
                if (token2 == null)
                    return Results.Ok("Token not acquired");
                else
                    return Results.Problem("Token was acquired, it should not have been");
            }
        }
    }
}).WithName("denyalock");

app.Run();

public class SampleEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
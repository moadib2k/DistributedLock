using Amazon.DynamoDBv2;
using MRB.DistributedLock.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MRB.DistributedLock
{
    public static class Registration
    {

        public static IServiceCollection AddDynamoDistributedLock(this IServiceCollection services, string tableName)
        {    
            //Add AWS Dynamo
            services.AddAWSService<IAmazonDynamoDB>()
                //Add the repo options, normally this would be populated from config
                .AddSingleton(new DynamoDbRepositoryOptions { TableName = tableName})
                //Add a repo
                .AddTransient<ILockRepostory, DynamoDbRepository>()
                //finally add the client that will use the repo
                .AddTransient<ILockClient, LockClient>();

            return services;
        }
    }
}

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Internal.Util;
using Amazon.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MRB.DistributedLock.Repository
{

    public class DynamoDbRepository : ILockRepostory
    {
        private IAmazonDynamoDB _dynamoDbClient;
        private ILogger<DynamoDbRepository> _logger;
        private readonly DynamoDbRepositoryOptions _options;

        public DynamoDbRepository(DynamoDbRepositoryOptions options, IAmazonDynamoDB dynamoDbClient, ILogger<DynamoDbRepository> logger)
        {
            _dynamoDbClient = dynamoDbClient ?? throw new ArgumentNullException(nameof(dynamoDbClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<bool> TryCreateTokenAsync(ILockToken token)
        {
            try
            {
                _logger.LogTrace("Creating Token {id}", token.LockId);

                //The lock will auto delete in 1 day
                var item = LockToken.ToItem(token);
                item.Add("ttl", new AttributeValue { N = AWSSDKUtils.ConvertToUnixEpochSeconds(DateTime.UtcNow.AddDays(1)).ToString() });

                PutItemRequest request = new PutItemRequest
                {
                    TableName = _options.TableName,
                    Item = LockToken.ToItem(token),
                    //we wont put an item if the instance is already present
                    ConditionExpression = "attribute_not_exists(InstanceId) AND attribute_not_exists(InstanceType)",
                };

                //Put the record
                var response = await _dynamoDbClient.PutItemAsync(request);
                return true;

            }
            catch (ConditionalCheckFailedException e)
            {
                //The lock already exists
                _logger.LogInformation(e, "Tried to create duplicate lock {id}", token.LockId);
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error trying to create token {id}", token.LockId);
                throw;
            }
        }

        public async Task<bool> TryDeleteTokenAsync(ILockToken token)
        {
            try
            {
                _logger.LogTrace("Deleting Token {id}", token.LockId);

                DeleteItemRequest request = new DeleteItemRequest
                {
                    TableName = _options.TableName,
                    Key = new Dictionary<string, AttributeValue> {
                        { "InstanceId", new AttributeValue { S = token.InstanceId.ToString() } },
                        { "InstanceType", new AttributeValue { S = token.InstanceType } }
                    },
                    ConditionExpression = "LockId=:lockId",
                    ExpressionAttributeValues = new() {
                        {":lockId", new AttributeValue{S=token.LockId.ToString()} }
                    }
                };

                var response = await Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
                   .ExecuteAsync(() => _dynamoDbClient.DeleteItemAsync(request));

                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (ConditionalCheckFailedException e)
            {
                _logger.LogWarning(e, "Attempt to delete token with mismatched LockId {id}", token.LockId);
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error trying to delete token");
                throw;
            }
        }

        public async Task<ILockToken?> TryGetTokenAsync(Type instanceType, Guid instanceId)
        {
            try
            {
                _logger.LogTrace("Retrieving token for {nane} {id}", instanceType.Name, instanceId);

                GetItemRequest request = new GetItemRequest
                {
                    TableName = _options.TableName,
                    ConsistentRead = true,
                    Key = new Dictionary<string, AttributeValue> {
                        { "InstanceId", new AttributeValue { S = instanceId.ToString() } },
                        { "InstanceType", new AttributeValue { S = instanceType.Name } }
                    },
                    AttributesToGet = LockToken.Attributes
                };

                var response = await _dynamoDbClient.GetItemAsync(request);
                if (!response.IsItemSet)
                    return null;

                LockToken rv = LockToken.FromItem(response.Item);

                return rv;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error trying to get token");
                throw;
            }
        }

        public async Task<(bool success, uint version)> TryUpdateTokenAsync(ILockToken token)
        {
            try
            {
                _logger.LogTrace("Updating Token {id}", token.LockId);

                var item = LockToken.ToItem(token);

                var request = new UpdateItemRequest
                {
                    TableName = _options.TableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "InstanceId", item["InstanceId"] },
                        { "InstanceType", item["InstanceType"] }
                    },
                    ConditionExpression = "#lockId=:lockId",
                    UpdateExpression = "SET #expires = :expires, #version = #version + :increment",
                    ExpressionAttributeNames = new()
                    {
                        { "#expires", "Expires" },
                        { "#version", "Version" },
                        { "#lockId", "LockId" }
                    },
                    ExpressionAttributeValues = new() {
                        {":expires", item["Expires"]},
                        {":lockId", item["LockId"]},
                        {":increment", new AttributeValue{N=1.ToString()} }
                    },
                    ReturnValues = ReturnValue.UPDATED_NEW
                };

                var response = await _dynamoDbClient.UpdateItemAsync(request);
                var newVersion = uint.Parse(response.Attributes["Version"].N);
                return (true, newVersion);
            }
            catch (ConditionalCheckFailedException e)
            {
                _logger.LogError(e, "Update token failed on lock mismatch {id}", token.LockId);
                return (false, token.Version);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error trying to update token");
                throw;
            }
        }
    }
}

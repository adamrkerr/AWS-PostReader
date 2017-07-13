using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using PostReader.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PostReader_NewPost
{
    public class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(NewPostEvent input, ILambdaContext context)
        {
            var recordId = Guid.NewGuid().ToString();

            var voice = input.Voice;
            var text = input.Text;

            var tableName = Environment.GetEnvironmentVariable("DB_TABLE_NAME");

            var topicARN = Environment.GetEnvironmentVariable("SNS_TOPIC");

            Console.WriteLine($"Generating new DynamoDB record, with ID: {recordId}");
            Console.WriteLine($"Input Text: {text}");
            Console.WriteLine($"Selected Voice: {voice}");

            using (var dynamoDb = new AmazonDynamoDBClient())
            {
                await dynamoDb.PutItemAsync(
                    tableName,
                    new Dictionary<string, AttributeValue>
                        {
                        {"id", new AttributeValue {S = recordId}},
                        {"voice", new AttributeValue {S = voice}},
                        {"text", new AttributeValue {S = text}},
                        {"status", new AttributeValue {S = "PROCESSING"}}
                        }
                );
            }

            using (var sns = new AmazonSimpleNotificationServiceClient())
            {
                await sns.PublishAsync(topicARN, recordId);
            }

            return recordId;
        }
    }
}

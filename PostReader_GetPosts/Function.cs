using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using PostReader.Models;
using Amazon.DynamoDBv2;
using Amazon;
using Amazon.DynamoDBv2.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PostReader_GetPosts
{
    public class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PostRecord>> FunctionHandler(PostRequestEvent input, ILambdaContext context)
        {
            var recordId = input.RecordId;

            var tableName = Environment.GetEnvironmentVariable("DB_TABLE_NAME");

            using (var dynamoDb = new AmazonDynamoDBClient())
            {

                var fieldsToRetrieve = new List<string> { "id", "text", "voice", "status", "url" };

                if (recordId == "*")
                {
                    var results = await dynamoDb.ScanAsync(tableName, fieldsToRetrieve);

                    return results.Items.Select(d =>
                    {
                        var record = new PostRecord
                        {
                            Id = d["id"].S,
                            Text = d["text"].S,
                            Voice = d["voice"].S,
                            Status = d["status"].S,
                            Url = d["url"].S
                        };

                        return record;
                    }).ToList();
                }
                else
                {
                    var result = await dynamoDb.GetItemAsync(tableName,
                        new Dictionary<string, AttributeValue>
                            {{"id", new AttributeValue {S = recordId}}}
                    );

                    var record = new PostRecord
                    {
                        Id = result.Item["id"].S,
                        Text = result.Item["text"].S,
                        Voice = result.Item["voice"].S,
                        Status = result.Item["status"].S,
                        Url = result.Item["url"].S
                    };

                    return new[] { record };
                }
            }
        }
    }
}

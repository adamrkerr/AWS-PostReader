using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.DynamoDBv2;
using Amazon;
using Amazon.DynamoDBv2.Model;
using PostReader.Models;
using Amazon.Polly;
using Amazon.Polly.Model;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PostReader_ConvertAudio
{
    public class Function
    {
        
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(SNSEvent snsEvent, ILambdaContext context)
        {
            var postId = snsEvent.Records[0].Sns.Message;

            Console.WriteLine($"Text to Speech function. Post ID in DynamoDB: {postId}");

            //Retrieving information about the post from DynamoDB table
            PostRecord record;

            using (var dynamoDb = new AmazonDynamoDBClient())
            {
                var fieldsToRetrieve = new List<string> { "id", "text", "voice", "status" };

                var tableName = Environment.GetEnvironmentVariable("DB_TABLE_NAME");

                var result = await dynamoDb.GetItemAsync(tableName,
                    new Dictionary<string, AttributeValue>
                        {{"id", new AttributeValue {S = postId}}}
                );

                record = new PostRecord
                {
                    Id = result.Item["id"].S,
                    Text = result.Item["text"].S,
                    Voice = result.Item["voice"].S,
                    Status = result.Item["status"].S
                };
            }

            var outputPath = String.Format("{0}tmp{0}{1}.mp3", Path.DirectorySeparatorChar, record.Id);

            var rest = record.Text;

            // Because single invocation of the polly synthesize_speech api can 
            // transform text with about 1,500 characters, we are dividing the 
            // post into blocks of approximately 1,000 characters.
            var textBlocks = new List<string>();

            while (rest.Length > 1100)
            {
                var end = rest.IndexOf(".", 0, 1000);

                if(end < 0)
                {
                    end = rest.IndexOf(" ", 0, 1000);
                }

                textBlocks.Add(rest.Substring(0, end + 1));

                if(end + 1 < rest.Length)
                {
                    rest = rest.Substring(end + 1);
                }
            }

            if (!String.IsNullOrEmpty(rest))
            {
                textBlocks.Add(rest);
            }

            //For each block, invoke Polly API, which will transform text into audio
            using (var polly = new AmazonPollyClient())
            {

                foreach (var textBlock in textBlocks)
                {
                    var request = new SynthesizeSpeechRequest
                    {
                        OutputFormat = "mp3",
                        Text = textBlock,
                        VoiceId = record.Voice //TODO error prone
                    };

                    var pollyResponse = await polly.SynthesizeSpeechAsync(request);

                    if (pollyResponse.AudioStream != null
                        && pollyResponse.AudioStream.CanRead)
                    {
                        using (var tempFile = File.Open(outputPath, FileMode.Append))
                        {
                            await pollyResponse.AudioStream.CopyToAsync(tempFile);
                        }
                    }
                }
            }

            var bucketName = Environment.GetEnvironmentVariable("BUCKET_NAME");

            string url;

            using (var s3Client = new AmazonS3Client())
            {
                var key = Path.GetFileName(outputPath);

                var s3Request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    FilePath = outputPath,
                    CannedACL = S3CannedACL.PublicRead,
                    Key = key
                };

                var s3Result = await s3Client.PutObjectAsync(s3Request);

                var location = await s3Client.GetBucketLocationAsync(bucketName);

                var region = location.Location;

                url = "https://s3";

                if (!String.IsNullOrEmpty(region))
                {
                    url = url + "-" + region;
                }

                url = url + ".amazonaws.com/"
                    + bucketName
                    + "/"
                    + key;
                                
            }

            //Updating the item in DynamoDB
            using (var dynamoDb = new AmazonDynamoDBClient())
            {
                var tableName = Environment.GetEnvironmentVariable("DB_TABLE_NAME");

                var result = await dynamoDb.UpdateItemAsync(tableName,
                    new Dictionary<string, AttributeValue>
                        {{"id", new AttributeValue {S = postId}}},
                    new Dictionary<string, AttributeValueUpdate>
                        {
                            { "status", new AttributeValueUpdate(new AttributeValue {S = "UPDATED"}, AttributeAction.PUT) },
                            {"url", new AttributeValueUpdate(new AttributeValue {S = url}, AttributeAction.PUT) }
                    }
                );

            }

            return "success";
        }
    }
}

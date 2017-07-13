# AWS Lambda Audio Conversion Function

This project consists of:
* Function.cs - class file containing a class with a single function handler method
* aws-lambda-tools-defaults.json - default argument settings for use with Visual Studio and command line deployment tools for AWS

## AWS Technologies Demonstrated
* Lambda function receiving SNS events
* Retrieving a record from DynamoDB
* Submitting text to Polly for conversion to audio
* Storing a file in S3
* Updating a record in DynamoDB
using Amazon.Runtime;
using AWS.Logger;
using AWS.Logger.SeriLog;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Json;

namespace CaseManager.Loggers;

public static class SerilogFactories
{
    public static Logger CreateCloudWatchLogger(IConfiguration configuration)
    {
        var accessKey = configuration["Aws:AccessKey"];
        var secretKey = configuration["Aws:SecretKey"];
        var logGroup = configuration["Aws:LogGroup"];
        var region = configuration["Aws:Region"];
        var logStreamPrefix = configuration["Aws:LogStreamPrefix"];

        var awsLoggerConfig = new AWSLoggerConfig
        {
            Region = region,
            Credentials = new BasicAWSCredentials(accessKey, secretKey),
            LogGroup = logGroup,
            LogStreamNamePrefix = logStreamPrefix
        };

        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Seq("http://localhost:5341")
            .WriteTo.AWSSeriLog(awsLoggerConfig, textFormatter: new JsonFormatter())
            .CreateLogger();
    }

    public static Logger CreateSeqLogger(IConfiguration configuration)
    {
        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();
    }
}
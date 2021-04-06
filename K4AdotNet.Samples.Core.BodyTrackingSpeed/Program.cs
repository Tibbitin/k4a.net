﻿using K4AdotNet.Sensor;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace K4AdotNet.Samples.Core.BodyTrackingSpeed
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Sdk.ConfigureLogging(TraceLevel.Warning, logToStdout: true);
            Sdk.ConfigureRecordLogging(TraceLevel.Warning, logToStdout: true);
            Sdk.ConfigureBodyTrackingLogging(TraceLevel.Warning, logToStdout: true);

            Console.WriteLine("Body tracking speed test on prerecorded video from Kinect for Azure device");

            var processingParameters = args.Length == 0
                ? AskProcessingParameters()
                : ParseCommandLineArguments(args);

            if (processingParameters == null)
            {
                PrintHowToUse();
                return;
            }

            Console.WriteLine();
            Process(processingParameters);

            if (args.Length == 0)
            {
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
        }

        private static void PrintProcessingStatus(Processor processor)
        {
            Console.Write(new string('\b', 50));
            Console.Write(new string(' ', 50));
            Console.Write(new string('\b', 50));
            Console.Write($"processed: {processor.TotalFrameCount}    with body: {processor.FrameWithBodyCount}    in buffer: {processor.QueueSize}");
        }

        private delegate bool ParameterSetter(string value, [NotNullWhen(returnValue: false)] out string? message);

        #region Asking parameters from STDIN

        private static ProcessingParameters? AskProcessingParameters()
        {
            Console.WriteLine("No command line arguments specified.");
            Console.WriteLine("Please enter execution parameters:");
            var parameters = new ProcessingParameters();
            if (!AskParameter(ProcessingParameters.MkvPathDescription, parameters.TrySetMkvPath))
                return null;
            if (!AskParameter(ProcessingParameters.ProcessingModeDescription, parameters.TrySetProcessingMode))
                return null;
            if (!AskParameter(ProcessingParameters.DnnModelDescription, parameters.TrySetDnnModel))
                return null;
            if (!AskParameter(ProcessingParameters.ImplementationDescription, parameters.TrySetImplementation))
                return null;
            if (!AskParameter(ProcessingParameters.StartTimeDescription, parameters.TrySetStartTime))
                return null;
            if (!AskParameter(ProcessingParameters.EndTimeDescription, parameters.TrySetEndTime))
                return null;
            return parameters;
        }

        private const int MAX_ASK_ATTEMPTS = 3;

        private static bool AskParameter(string prompt, ParameterSetter setter)
        {
            for (var attempt = 0; attempt < MAX_ASK_ATTEMPTS; attempt++)
            {
                Console.Write(prompt+ ": ");
                var str = Console.ReadLine();
                if (setter(str, out var message))
                    return true;
                Console.WriteLine(message);
                Console.Beep();
            }
            return false;
        }

        #endregion

        #region Parsing parameters from command-line arguments

        private static ProcessingParameters? ParseCommandLineArguments(string[] args)
        {
            var parameters = new ProcessingParameters();

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i].Trim();
                switch (arg.ToLowerInvariant())
                {
                    case "-m":
                    case "--mode":
                        if (!ParseArgument(args, ref i, parameters.TrySetProcessingMode))
                            return null;
                        break;
                    case "-d":
                    case "--dnnModel":
                        if (!ParseArgument(args, ref i, parameters.TrySetDnnModel))
                            return null;
                        break;
                    case "-i":
                    case "--implementation":
                        if (!ParseArgument(args, ref i, parameters.TrySetImplementation))
                            return null;
                        break;
                    case "-s":
                    case "--startTime":
                        if (!ParseArgument(args, ref i, parameters.TrySetStartTime))
                            return null;
                        break;
                    case "-e":
                    case "--endTime":
                        if (!ParseArgument(args, ref i, parameters.TrySetEndTime))
                            return null;
                        break;
                    default:
                        if (!parameters.TrySetMkvPath(arg, out var message))
                        {
                            Console.WriteLine($"Invalid command-line argument \"{arg}\":");
                            if (arg.StartsWith("-") && !ProcessingParameters.IsValueLikeToMkvFilePath(arg))
                                message = "Unknown option " + arg;
                            Console.WriteLine(message);
                            return null;
                        }
                        break;
                }
            }

            return parameters;
        }

        private static bool ParseArgument(string[] args, ref int argIndex, ParameterSetter setter)
        {
            var name = args[argIndex];
            var value = argIndex + 1 < args.Length ? args[argIndex + 1] : null;

            string? message = null;
            if (value != null && setter(value, out message))
            {
                argIndex++;
                return true;
            }

            if (value == null || value.StartsWith("-") || ProcessingParameters.IsValueLikeToMkvFilePath(value))
            {
                Console.WriteLine("Invalid command-line arguments:");
                Console.WriteLine($"Option {name} must be followed by its value");
                return false;
            }

            argIndex++;
            Console.WriteLine($"Invalid value \"{value}\" of command-line option {name}:");
            Console.WriteLine(message);
            return false;
        }

        #endregion

        #region How to use

        private static void PrintHowToUse()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: ");
            Console.WriteLine("dotnet K4AdotNet.Samples.BodyTrackingSpeedTest.dll [options] <mkvFile>");
            Console.WriteLine("where: ");
            Console.WriteLine("  <mkvFile> - " + ProcessingParameters.MkvPathDescription);
            Console.WriteLine("  options:");
            Console.WriteLine("    -m, --mode c|g|u|t|d\t\t" + ProcessingParameters.ProcessingModeDescription);
            Console.WriteLine("    -d, --dnnMode d|l\t\t" + ProcessingParameters.DnnModelDescription);
            Console.WriteLine("    -i, --implementation s|p|e\t\t" + ProcessingParameters.ImplementationDescription);
            Console.WriteLine("    -s, --startTime <time>\t\t" + ProcessingParameters.StartTimeDescription);
            Console.WriteLine("    -e, --endTime <time>\t\t" + ProcessingParameters.EndTimeDescription);
            Console.WriteLine();
            Console.WriteLine("When no command-line arguments specified, STDIN and STDOUT are used to ask parameters");
            Console.WriteLine();
        }

        #endregion

        #region Processing

        private static void Process(ProcessingParameters processingParameters)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("opening recording and creating body tracking pipeline...");
                using (var processor = Processor.Create(processingParameters))
                {
                    Console.WriteLine("opened:");
                    Console.WriteLine("  depth mode = " + processor.RecordConfig.DepthMode);
                    Console.WriteLine("  camera frame rate = " + processor.RecordConfig.CameraFps.ToNumberHz());
                    Console.WriteLine("  record length = " + processor.RecordLength);
                    Console.WriteLine("processing frames:");
                    var sw = Stopwatch.StartNew();
                    while (processor.NextFrame())
                    {
                        PrintProcessingStatus(processor);
                    }
                    sw.Stop();
                    PrintProcessingStatus(processor);
                    Console.WriteLine();
                    Console.WriteLine("done!");
                    if (sw.Elapsed.TotalSeconds > 0)
                    {
                        var trackingSpeed = processor.TotalFrameCount / sw.Elapsed.TotalSeconds;
                        Console.WriteLine($"tracking speed = {trackingSpeed} FPS");
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine();
                Console.WriteLine("ERROR!");
                Console.WriteLine(exc.ToString());
            }
        }

        #endregion
    }
}

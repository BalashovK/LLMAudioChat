using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using static System.Net.Mime.MediaTypeNames;

namespace LLMAudioChat
{
    public class SpeechToText : IDisposable
    {
        public GgmlType ModelType { get; set; } = GgmlType.Base;

        public string last_string = "";
        WhisperProcessor processor;
        WhisperFactory factory;
        public string ModelName { get; set; } = "en-us-base.ggml";  

        public SpeechToText(string a_modelName)
        {
            ModelName = a_modelName;
            if (!File.Exists(ModelName))
            {
                Console.WriteLine($"Downloading Model {ModelName}");
                using var modelStream = WhisperGgmlDownloader.GetGgmlModelAsync(ModelType).Result;
                using var fileWriter = File.OpenWrite(ModelName);
                modelStream.CopyToAsync(fileWriter);
            }

            // Same factory can be used by multiple task to create processors.
            factory = WhisperFactory.FromPath(ModelName);

            WhisperProcessorBuilder builder = factory.CreateBuilder()
                .WithLanguage("en");

            processor = builder.Build();
        }

        public async Task<string> S2T(Stream stream)
        {
            StringBuilder stringBuilder = new StringBuilder();
//            stream.Position = 0;

//            string fn = @"D:\Whisper\With_NAudio\Test\test_chat_test.wav";
//            string fn= @"D:\Whisper\With_NAudio\output.wav";
//            FileStream fileStream = File.OpenRead(fn);

            await foreach (var segment in processor.ProcessAsync(stream, CancellationToken.None))
            {
                Console.WriteLine($"New Segment: {segment.Start} ==> {segment.End} : {segment.Text}");
                stringBuilder.Append(segment.Text + " ");
            }
            last_string = stringBuilder.ToString();
            return last_string;
        }
        public void Dispose()
        {
            processor?.Dispose();
            factory?.Dispose();
        }
    }
}

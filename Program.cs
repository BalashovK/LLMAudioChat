using System.Diagnostics;
using System.Speech.Synthesis;

namespace LLMAudioChat
{
    internal class Program
    {

        static int cnt = 0;
        static SpeechToText s2t;
        static ChatAI ai;

        const bool diagnostics = false;

        static CancellationTokenSource cts;
        static TextToSpeech tts;

        static string LLM_Model_Path = @"D:\Llama\models\mistral-11b-omnimix-bf16.Q5_K_M.gguf";
        static string LLM_AI_Name = "David";
        static float mic_threshold = 0.0015f; // below is quiet, above is user speeking

        static void Main(string[] args)
        {
            ai = new ChatAI(LLM_Model_Path, LLM_AI_Name);
            s2t = new SpeechToText("en-us-base.ggml");
            tts = new TextToSpeech();

            Console.WriteLine("Initialized");
            AudioLevelMonitor audioMonitor = new AudioLevelMonitor(mic_threshold);

            audioMonitor.OnWavAudioStreamReady += OnAudio;

            audioMonitor.StartRecording();
            Console.WriteLine("I am listening! Press Esc to quit.");
            while (true)
            {
                Thread.Sleep(100);
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                {
                    cts?.Cancel();
                    break;
                }
            }
            audioMonitor.StopRecording();
        }
        private static void OnAudio(object sender, MemoryStreamEventArgs e)
        {
            // stop ChatAI
            cts?.Cancel();

            e.MemoryStream.Position = 0;

            Stream stream = e.MemoryStream;

            e.MemoryStream.Position = 0;
            string txt = Task.Run(async () => await s2t.S2T(stream)).Result;
            txt = txt.Replace("[BLANK_AUDIO]", "");
            Console.WriteLine($"question: {txt}");

            // wait for ChatAI to finish
            if (cts != null)
            {
//                Console.WriteLine("Wait for Ai to stop");
                Thread.Sleep(250);
            }
            cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            IEnumerable<string> result = ai.GetResponse(txt, ct);
            tts.Speak(result, ct);
        }
    }
}


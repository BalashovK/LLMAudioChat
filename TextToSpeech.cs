using NAudio.Dmo.Effect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace LLMAudioChat
{
    public class TextToSpeech
    {
        StringBuilder sb = new StringBuilder();
        SpeechSynthesizer synthesizer;
        Queue<string> queue = new Queue<string>();

        CancellationToken m_ct;

         public TextToSpeech()
         {
            Initialize();
         }
        public void Initialize()
        {
            synthesizer = new SpeechSynthesizer();
            synthesizer.SetOutputToDefaultAudioDevice();
            synthesizer.Rate = 1;

            Task.Run(async () => await SpeakTask());
        }
        public void Speak(IEnumerable<string> text_stream, CancellationToken ct)
        {
            m_ct = ct;
            string last_chunk = "";
            foreach (string s in text_stream)
            {
                if (ct.IsCancellationRequested)
                {
                    Console.WriteLine("<<<Cancellation requested>>>");
                    queue.Clear();
                    sb.Clear();
                    return;
                }

                bool last_chunk_was_punctuation = (last_chunk.EndsWith('.') || last_chunk.EndsWith('!') || last_chunk.EndsWith('?') || last_chunk.EndsWith(';'));
                bool this_chunk_is_space = (s.StartsWith(' ') || s.StartsWith('\n') || s.StartsWith('\r') || s.StartsWith('\t'));

                bool split = (s==" and" || s==" or" || s==" but" || s==" so" || (last_chunk_was_punctuation && this_chunk_is_space));
                if (!split)
                {
                    sb.Append(s);

                    Console.Write(s);
                }
                else
                {
                    string text_to_speak = sb.ToString();
                    queue.Enqueue(text_to_speak);
                    sb.Clear();
                    sb.Append(s);
                    Console.Write(s);
                }

                last_chunk = s;
            }
            queue.Enqueue(sb.ToString());
            sb.Clear();
        }
        private async Task SpeakTask()
        {
            while (true)
            {
                if(queue.Count > 0)
                {
                    StringBuilder sb2 = new StringBuilder();
                    while (queue.Count > 0)
                    {
                        sb2.Append(queue.Dequeue());
                    }
                    string string_to_speek = sb2.ToString();
                    if (string_to_speek.ToLower().EndsWith("user:"))
                    {
                        string_to_speek = string_to_speek.Substring(0, string_to_speek.Length - 5);
                    }

                    Prompt p = synthesizer.SpeakAsync(string_to_speek);

                    // wait for completion
                    while (!p.IsCompleted)
                    {
                        if (m_ct.IsCancellationRequested)
                        {
//                            Console.WriteLine("<<<Cancellation requested>>>");
                            synthesizer.SpeakAsyncCancelAll();
                            queue.Clear();
                            break;
                        }
                        await Task.Delay(10);
                    }
                    
                }
                else
                {
                    await Task.Delay(10);
                }
            }
        }
    }
}

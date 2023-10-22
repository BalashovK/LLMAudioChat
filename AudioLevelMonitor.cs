using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLMAudioChat
{

    // define custom EventArgs with MemoryStream called MemoryStreamEventArgs
    public class MemoryStreamEventArgs : EventArgs
    {
        public MemoryStream MemoryStream { get; set; }
    }
    public class AudioLevelMonitor
    {
        private const int SampleRate = 16000; // 16 kHz
        private const int SampleRate_ms = SampleRate / 1000;
        private const int BitsPerSample = 16; // 8 bits
        private const int Channels = 1; // mono
        private const int BytesPerSample = BitsPerSample / 8;
        private const int BufferSize_ms = 4 * (QuietTime_ms + LoudTime_ms);
        private const int BufferSize_bytes = SampleRate_ms * BytesPerSample * Channels * BufferSize_ms;
        private const int WaveBufferSize_ms = 100;
        private const int WaveBufferSize_bytes = BytesPerSample * WaveBufferSize_ms * SampleRate_ms;
        private const int AudioLevelBufferSize_bins = BufferSize_ms / WaveBufferSize_ms;

        private const int QuietTime_ms = 1000;
        private const int LoudTime_ms = 1000;

        private const int QuietTime_bins = QuietTime_ms / WaveBufferSize_ms;
        private const int LoudTime_bins = LoudTime_ms / WaveBufferSize_ms;


        private int CurrentPositionInAudioLevel = 0;

        private float threshold; // below is quiet, above is loud


        private byte[] circularBuffer;
        private int writePosition = 0;
        private WaveInEvent waveIn;

        MemoryStream PCM_ms;

        public event EventHandler<MemoryStreamEventArgs> OnWavAudioStreamReady;

        private bool IsRecording { get; set; } = false;

        public bool IsLoud { get; private set; } = false; // analyzer mode
        public float[] AudioLevel { get; private set; } 
        public long TotalBytesRecorded { get; private set; } = 0;

        public int ms2bytes(int ms)
        {
            return ms * SampleRate_ms * BytesPerSample * Channels;
        }
        public int bytes2ms(int bytes)
        {
            return bytes / (SampleRate_ms * BytesPerSample * Channels);
        }

        public int ms2samples(int ms)
        {
            return ms * SampleRate_ms;
        }
        public int samples2ms(int samples)
        {
            return samples / SampleRate_ms;
        }

        public AudioLevelMonitor(float a_threshold)
        {
            threshold = a_threshold;    
            CurrentPositionInAudioLevel = 0;
            circularBuffer = new byte[BufferSize_bytes];
            // fill CircularBuffer with 0
            for (int i = 0; i < BufferSize_bytes; i++)
            {
                circularBuffer[i] = 0;
            }
            AudioLevel = new float[AudioLevelBufferSize_bins];
            // fill AudioLevel with 0
            for (int i = 0; i < AudioLevelBufferSize_bins; i++)
            {
                AudioLevel[i] = 0;
            }

            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels);
            //waveIn.DeviceNumber = 0;
            waveIn.BufferMilliseconds = WaveBufferSize_ms;
            waveIn.DataAvailable += OnDataAvailable;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded != WaveBufferSize_bytes)
            {
                Console.WriteLine($"OnDataAvailable: e.BytesRecorded = {e.BytesRecorded} WaveBufferSizeInBytes: {WaveBufferSize_bytes} mismatch");
                return;
            }
            float sum = 0;
            for (int i = 0; i < e.BytesRecorded; i++)
            {
                circularBuffer[writePosition] = e.Buffer[i];
                writePosition = (writePosition + 1) % BufferSize_bytes;
                TotalBytesRecorded++;
            }
            for (int i = 0; i < e.BytesRecorded; i += BytesPerSample)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i);
                sum += Math.Abs(sample);
            }

            sum /= e.BytesRecorded;
            CurrentPositionInAudioLevel = (int)(TotalBytesRecorded % circularBuffer.LongLength) / WaveBufferSize_bytes;
            if (CurrentPositionInAudioLevel >= AudioLevel.Length)
            {
                Console.WriteLine($"OnDataAvailable: CurrentPositionInAudioLevel = {CurrentPositionInAudioLevel} AudioLevel.Length: {AudioLevel.Length} mismatch");
                return;
            }
            AudioLevel[CurrentPositionInAudioLevel] = sum / 32768f;

            Analyzer();

            if (IsLoud)
            {
                // copy to memory stream PCM_ms
                PCM_ms.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

        public void StartRecording()
        {
            waveIn.StartRecording();
            IsRecording = true;
        }

        public void StopRecording()
        {
            waveIn.StopRecording();
            IsRecording = false;
        }

        // analyzer

        private void Analyzer()
        {
            if (IsLoud)
            {
                if (CheckForQuiet())
                {
                    Task.Run(() => FireOnWavAudioStreamReady());
                    IsLoud = false; // stop recording to MemoryStream
                    Console.WriteLine("Analyzer: Quiet");

                }
            }
            else
            {
                if (CheckForLoud())
                {
                    PCM_ms = new MemoryStream();
                    // copy from circular buffer to PCM_ms
                    CopyLoudFromCircularBufferToPCM_ms();
                    IsLoud = true; // start recording to MemoryStream
                    Console.WriteLine("Analyzer: Loud");
                }
            }
        }
        
        private void CopyLoudFromCircularBufferToPCM_ms()
        {
            // find starting position - go back (loud time + quiet time)
            int start = (int)(TotalBytesRecorded % (long)BufferSize_bytes) - (ms2bytes(LoudTime_ms) + ms2bytes(QuietTime_bins));
           

            if (start < 0)
            {
                start += BufferSize_bytes;
            }

            int how_many_to_copy = ms2bytes(LoudTime_ms);
            // last buffer will be copied, so let's subtract it
            how_many_to_copy -= ms2bytes(WaveBufferSize_ms);

            for (int i = 0; i < how_many_to_copy; i++)
            {
                PCM_ms.WriteByte(circularBuffer[start]);
                start = (start + 1) % BufferSize_bytes;
            }
        }

        void FireOnWavAudioStreamReady()
        {
            if (OnWavAudioStreamReady != null)
            {
                MemoryStream wavStream = ConvertPCMToWav(PCM_ms);
                MemoryStreamEventArgs args = new MemoryStreamEventArgs();
                args.MemoryStream = wavStream;
                OnWavAudioStreamReady?.Invoke(this, args);
            }
        }

        private bool CheckForQuiet()
        {
            if (CountLoud(QuietTime_bins) == 0)
            {
                return true;
            }
            else
            { 
                return false; 
            }
        }
        private bool CheckForLoud()
        {
            if (CountLoud(LoudTime_bins) > (LoudTime_bins * 3) / 4) // 75% of bins
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public int CountLoud(int bins)
        {
            int idx = CurrentPositionInAudioLevel;
            int loud_cnt = 0;
            for (int i=0; i<QuietTime_bins; i++)
            {
                if (AudioLevel[idx] > threshold)
                {
                    loud_cnt++;
                }

                idx--;
                if (idx < 0)
                {
                    idx = AudioLevelBufferSize_bins - 1;
                }
            }
            return loud_cnt;
        }

        public MemoryStream ConvertPCMToWav(MemoryStream pcmStream)
        {
            pcmStream.Position = 0;
            MemoryStream wavStream = new MemoryStream();

            WaveFormat waveFormat = new WaveFormat(16000, 16, 1);
            RawSourceWaveStream rawSource = new RawSourceWaveStream(pcmStream, waveFormat);

            WaveFileWriter waveWriter = new WaveFileWriter(wavStream, rawSource.WaveFormat);

            byte[] buffer = new byte[1024];
            int bytesRead;
            while ((bytesRead = rawSource.Read(buffer, 0, buffer.Length)) > 0)
            {
                waveWriter.Write(buffer, 0, bytesRead);
            }

            // Update WAV header with correct RIFF chunk size
            long dataSize = pcmStream.Length;
            wavStream.Position = 4;
            byte[] sizeBytes = System.BitConverter.GetBytes((uint)dataSize);
            wavStream.Write(sizeBytes, 0, 4);

            wavStream.Position = 0x2A;
            wavStream.Write(sizeBytes, 0, 4);

            wavStream.Position = 0;
            return wavStream;
        }
    }
}

# LLMAudioChat
Audio chat with GGUF LLM on Windows / C# 

## How to use it
1. Get a Windows computer with Visual Studio and NVidia graphics card. I use 3090, but lower end card would work too. Get headphones and a microphone (or maybe you have a speeker with microphone array which subtracts speaker from microphne signal? Because this software is not capable of doing that, sorry!)
2. Download an LLM in GGUF format. You can doenload it from HuggingFace. Just search models for "gguf". I like MistralLite 7B and Mistral 11B - they are fast. User TheBloke does excellent job quantizing them. I use quantization Q5_K-M, but you can use Q4_K_M if your graphics card is lower end.
3. In program.cs replace static string LLM_Model_Path = @"D:\Llama\models\mistral-11b-omnimix-bf16.Q5_K_M.gguf"; with path to the gguf file you downloaded.
4. Start the application, wait for prompt "I am listening! Press Esc to quit." and start asking questions. AI will anstwer you once you stop speaking. You can interrupt the AI.

First start will be slower because it will be downloading Whisperer (speech to text) neural network en-us-base.ggml which is 141MB.
Subsequent software restarts are fast.

## Troubleshooting
1. LLM is returning some garbage
Perhaps, it cannot hear you. Make sure Whisperes downloaded en-us-base.ggm to your binary folder and the file is ~141MB, and not a few kilobytes
Also, make sure you see text "Loud" when you speek, and "Quiet" when you stop speeking.
Adjust mic_threshold value to match your microphine sensitivity. For example, if you never see "Loud", increase this value 2x until you see text "Loud" every time you speek.
The software will type recongnized text input to console.

2. LLM does not fit into graphics card memory.
Reduce number of layers loaded into GPU in ChatAI.cs (GpuLayerCount = 51).

3. How to test LLM in text mode?
There is excellent project LLamaSharp which works with gguf models.

## How it works
AudioLevelMonitor acquires audio from microphone with NAudio library and analyzes it for user audio input. It waits for user to start and then stop speaking.
Then Whisperer (SpeechToText.cs) converts it to text
Then LLM (ChatAI.cs) processes it and start streaming the response.
It goes to TextToSpeech which is Microsoft syntheser.

If you start speaking before it finished, previous response is aborted and a new one starts. I.e., you are welcome to interrupt the AI any time.


## Why David?
The AI name is "David" because I am using "David" voise from Microsoft. You are welcome to change this name. You can also use other voises and other languages.


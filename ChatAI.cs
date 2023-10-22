using LLama.Common;
using LLama;
using System.Speech.Synthesis;

namespace LLMAudioChat
{

    public class ChatAI
    {
        string AI_Name;

        string modelPath;

        LLamaWeights model;
        LLamaContext context;
        ChatSession session;
        InteractiveExecutor ex;
        public ChatAI(string a_modelPath, string a_AI_Name)
        {
            modelPath = a_modelPath;
            AI_Name = a_AI_Name;
            var parameters = new ModelParams(modelPath)
            {
                ContextSize = 8192,
                Seed = 1337,
                GpuLayerCount = 51
            };

            model = LLamaWeights.LoadFromFile(parameters);

            context = model.CreateContext(parameters);
            ex = new InteractiveExecutor(context);
            session = new ChatSession(ex);

            string prompt = $"Transcript of a dialog, where the User interacts with an Assistant named {AI_Name}. {AI_Name} is helpful, kind, honest, good at writing, and never fails to answer the User's requests immediately and with precision.\r\n\r\nUser: Hello, {AI_Name}.\r\n{AI_Name}: Hello. How may I help you today?\r\nUser: Please tell me what is Milky Way.\r\n{AI_Name}: Sure. A galaxy that contains our solar system.\r\nUser:";
            GetResponse(prompt, CancellationToken.None);
        }

        public IEnumerable<string> GetResponse(string prompt, CancellationToken ct)
        {
            return session.Chat(prompt, new InferenceParams() { Temperature = 0.0f, AntiPrompts = new List<string> { "User:" } }, ct);
        }
    }
}


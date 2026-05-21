using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;
namespace ScreenmateAi
{
    public class ChatStorageService
    {
        private readonly string _filePath = "conversations.json";

        public void Save(List<Conversation> conversations)
        {
            string json = JsonSerializer.Serialize(
                conversations,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(_filePath, json);
        }

        public List<Conversation> Load()
        {
            if (!File.Exists(_filePath))
            {
                return new List<Conversation>();
            }

            string json = File.ReadAllText(_filePath);

            return JsonSerializer.Deserialize<List<Conversation>>(json)
                   ?? new List<Conversation>();
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ElectricSketch.Model
{
    public static class Serialization
    {
        public static Schematic ReadFile(string path)
        {
            return JsonConvert.DeserializeObject<Schematic>(File.ReadAllText(path), new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
        }

        public static bool WriteFile(Schematic design, string filePath)
        {
            try
            {
                var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto
                });

                using (var textWriter = new StreamWriter(filePath))
                using (var jsonWriter = new JsonTextWriter(textWriter))
                    serializer.Serialize(jsonWriter, design);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

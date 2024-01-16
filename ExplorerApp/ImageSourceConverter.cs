using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace ExplorerApp
{
    public class SourceImageConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ImageSource);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.String)
            {
                string imagePath = token.ToObject<string>();
                return new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
            }
            // Handle other cases if needed
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Implement serialization logic if needed
            throw new NotImplementedException();
        }
    }
}

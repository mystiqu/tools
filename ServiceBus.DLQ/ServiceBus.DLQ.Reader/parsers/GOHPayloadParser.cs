using al.goh.common.domain.api.http;
using al.goh.common.domain.generic;
using al.goh.common.domain.po.iscala;
using al.goh.common.utilities.domain.edi.x12.Encode;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace ConsoleSBReader.parsers
{
    public class GOHPayloadParser : IParser
    {
        public ACTION _action = ACTION.NOTHING;
        public const string NAME = "GOH";

        public GOHPayloadParser()
        {
        }

        public GOHPayloadParser(ACTION action)
        {
            _action = action;
        }

        public string Parse(string content)
        {
            JObject jsonObject = JObject.Parse(content);
            JToken token = jsonObject["Content"];
            JToken body = token["Body"];
            content = body.ToString();

            //GOHHttpRequest gohHttpRequest = JsonConvert.DeserializeObject<GOHHttpRequest>(content, new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Ignore});
            //WrappedMessage wrappedMessage = GetWrappedMessageFromBody(content);

            jsonObject = JObject.Parse(Base64Decode(content));
            content = jsonObject["Payload"]["MessagePayload"].First["Data"].ToString();

            if (_action == ACTION.BASE64_ENCODE)
                content = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));
            else if (_action == ACTION.BASE64_DECODE)
                content = Base64Decode(content);

            //string data = wrappedMessage.Payload.MessagePayload[wrappedMessage.Payload.MessagePayload.Count - 1].Data;

            return content;
        }

        private string Base64Decode(string content)
        {
            byte[] wrappedMessageDataAsBytes = Convert.FromBase64String(content);
            string wrappedMessageDataAsString = Encoding.UTF8.GetString(wrappedMessageDataAsBytes);

            return wrappedMessageDataAsString;

        }
        private WrappedMessage GetWrappedMessageFromBody(string body)
        {
            byte[] wrappedMessageDataAsBytes = Convert.FromBase64String(body);
            string wrappedMessageDataAsString = Encoding.UTF8.GetString(wrappedMessageDataAsBytes);

            WrappedMessage wrappedMessage = al.goh.common.utilities.serialization.Serialization.JSONDeserializeUsingJSonConvert<WrappedMessage>(wrappedMessageDataAsString, MissingMemberHandling.Ignore);

            return wrappedMessage;
        }
    }
}

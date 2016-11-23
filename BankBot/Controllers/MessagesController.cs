using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using BankBot.Models;

namespace BankBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity) {
            if (activity.Type == ActivityTypes.Message) {
                /*    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    // calculate something for us to return
                    int length = (activity.Text ?? string.Empty).Length;

                    // return our reply to the user
                    Activity reply = activity.CreateReply($"You sent {activity.Text} which was {length} characters. Oh and you suck!");
                    await connector.Conversations.ReplyToActivityAsync(reply);  */

                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                LUISObject.RootObject luisObj;

                HttpClient client = new HttpClient();
                string x = await client.GetStringAsync(new Uri("https://api.projectoxford.ai/luis/v2.0/apps/a4ab66bf-68bb-4e95-8951-8b179e6f3c21?subscription-key=14b2e4303e5948578989704f3ef1fd87&q=" + activity.Text + "&verbose=true"));
                luisObj = JsonConvert.DeserializeObject<LUISObject.RootObject>(x);

                Activity reply = activity.CreateReply($"" + luisObj.topScoringIntent.intent);
                await connector.Conversations.ReplyToActivityAsync(reply);

            }
            else {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message) {
            if (message.Type == ActivityTypes.DeleteUserData) {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate) {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate) {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing) {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping) {
            }

            return null;
        }
    }
}

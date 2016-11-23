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

                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                LUISObject.RootObject luisObj;

                HttpClient client = new HttpClient();
                string x = await client.GetStringAsync(new Uri("https://api.projectoxford.ai/luis/v2.0/apps/a4ab66bf-68bb-4e95-8951-8b179e6f3c21?subscription-key=14b2e4303e5948578989704f3ef1fd87&q=" + activity.Text + "&verbose=true"));
                luisObj = JsonConvert.DeserializeObject<LUISObject.RootObject>(x);

                string intent = luisObj.topScoringIntent.intent;

                if (!userData.GetProperty<bool>("SendGreeting")) {
                    userData.SetProperty<bool>("SendGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    if (intent == "Greeting" || intent == "None") {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Hello! How can I help you?"));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    } else {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Hello!"));
                    }
                } else {
                    if (intent == "Greeting") {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Hello Again!"));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                }

                string output = "";

                if (intent == "None") {

                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Sorry I didn't quite understand what you meant."));
                    output = "Could you please phrase it differently?";

                } else if (intent == "FindBank") {

                    //Make use of GPS and GooglePlacesAPI to give distance to closest bank
                    //Use Cards
                    output = "You wish to find a bank...";

                } else if (intent == "CheckBalance") {

                    //Use the database to return amount of money in said bank account
                    output = "You wish to know how much money is in account " + luisObj.topScoringIntent.actions[0].parameters[0].value[0].entity + "...";

                } else if (intent == "MakePayment") {

                    //this will make the payment from one to the other
                    output = "You wish to make a payment of " + luisObj.topScoringIntent.actions[0].parameters[0].value[0].entity + " from your " + luisObj.topScoringIntent.actions[0].parameters[1].value[0].entity + " account to ";
                    if (luisObj.topScoringIntent.actions[0].parameters[2].value[0] != null) {
                        output += luisObj.topScoringIntent.actions[0].parameters[2].value[0].entity;
                    } else {
                        output += luisObj.topScoringIntent.actions[0].parameters[3].value[0].entity;
                    }

                }

                Activity reply = activity.CreateReply($"" + output);
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

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
using System.Collections.Generic;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

namespace BankBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        /// 

        private readonly string BANK_ADDRESS = "12 Grafton Road, Auckland";
        private readonly string BANK_LOGO = "https://s12.postimg.org/8pshbpbyl/Contoso_Bank_Logo.jpg";
        private readonly string BANK_MAP = "https://s16.postimg.org/pmxx1efed/Bank_Map.png";
        private readonly string CROSS_IMAGE = "https://s18.postimg.org/baqrd9j55/Cross.png";
        private readonly string TICK_IMAGE = "https://s14.postimg.org/v2q7rcstd/Tick.png";

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

                if (activity.Attachments.Count > 0) {
                    VisionServiceClient VisionServiceClient = new VisionServiceClient("fff4c0ce2c8946a1a4a6bd8b951d13c6");
                    AnalysisResult analysisResult = await VisionServiceClient.DescribeAsync(activity.Attachments[0].ContentUrl, 3);
                    activity.Text = analysisResult.Description.Captions[0].Text;
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"" + activity.Text));
                }

                HttpClient client = new HttpClient();
                string x = await client.GetStringAsync(new Uri("https://api.projectoxford.ai/luis/v2.0/apps/a4ab66bf-68bb-4e95-8951-8b179e6f3c21?subscription-key=14b2e4303e5948578989704f3ef1fd87&q=" + activity.Text + "&verbose=true"));
                luisObj = JsonConvert.DeserializeObject<LUISObject.RootObject>(x);

                string intent = luisObj.topScoringIntent.intent;

                if (activity.Text.Length > 9) {
                    if (activity.Text.Substring(0, 8).ToLower() == "location") {

                        string location = activity.Text.Substring(9);

                        DirectionsObject.RootObject dirObj;
                        string y = await client.GetStringAsync(new Uri("https://maps.googleapis.com/maps/api/directions/json?origin=" + location + "&destination=" + BANK_ADDRESS + "&key=AIzaSyBUytVVv7rrAWHM45JPuRgj52OEV_LwBnE"));
                        dirObj = JsonConvert.DeserializeObject<DirectionsObject.RootObject>(y);

                        string duration = dirObj.routes[0].legs[0].duration.text;
                        string distance = dirObj.routes[0].legs[0].distance.text;
                        string bankLocation = dirObj.routes[0].legs[0].end_address;

                        Activity locateReply = activity.CreateReply($"The distance to the closest bank is " + distance);
                        locateReply.Recipient = activity.From;
                        locateReply.Type = "message";
                        locateReply.Attachments = new List<Attachment>();

                        List<CardImage> cardImg = new List<CardImage>();
                        cardImg.Add(new CardImage(url: BANK_MAP));

                        List<CardAction> cardAct = new List<CardAction>();
                        cardAct.Add(new CardAction() {
                            Value = "https://www.google.co.nz/maps/dir/" + location + "/" + BANK_ADDRESS,
                            Type = "openUrl",
                            Title = "Open Google Maps"
                        });

                        HeroCard plCard = new HeroCard() {
                            Title = "Closest Bank",
                            Subtitle = "The closest bank is at " + BANK_ADDRESS + " and is " + duration + " away.",
                            Images = cardImg,
                            Buttons = cardAct
                        };

                        locateReply.Attachments.Add(plCard.ToAttachment());
                        await connector.Conversations.SendToConversationAsync(locateReply);

                        return Request.CreateResponse(HttpStatusCode.OK);

                    }
                }

                if (intent == "SetName") {

                    string newName = luisObj.topScoringIntent.actions[0].parameters[0].value[0].entity;
                    userData.SetProperty<string>("Name", " " + newName.Substring(0,1).ToUpper() + newName.Substring(1));
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Hello" + userData.GetProperty<string>("Name") + "!"));
                    return Request.CreateResponse(HttpStatusCode.OK);

                } else if (intent == "ClearData") {

                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Your data has been cleared"));
                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                if (!userData.GetProperty<bool>("SendGreeting")) {

                    userData.SetProperty<bool>("SendGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                    if (intent == "Greeting" || intent == "None") {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Hello" + userData.GetProperty<string>("Name") + "! How can I help you?"));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    } else {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Hello" + userData.GetProperty<string>("Name") + "!"));
                    }

                } else {

                    if (intent == "Greeting") {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Hello Again" + userData.GetProperty<string>("Name") + "!"));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                }

                string output = "";

                if (intent == "None") {

                    output = "Sorry I didn't quite understand what you meant. Could you please phrase it differently?";

                } else if (intent == "FindBank") {

                    output = "Please reply with \"location\" and then your current address";

                } else if (intent == "CheckBalance") {

                    string account = "spending";
                    if (luisObj.topScoringIntent.actions[0].parameters[0].value != null) {
                        account = luisObj.topScoringIntent.actions[0].parameters[0].value[0].entity;
                    }

                    double amount = 0; //Look in database

                    output = "You have " + amount + " in your " + account + " account";

                } else if (intent == "MakePayment") {

                    string amount = luisObj.topScoringIntent.actions[0].parameters[3].value[0].entity;
                    string account = luisObj.topScoringIntent.actions[0].parameters[0].value[0].entity;
                    string payee = "undefined";
                    if (luisObj.topScoringIntent.actions[0].parameters[1].value[0] != null) {
                        payee = luisObj.topScoringIntent.actions[0].parameters[1].value[0].entity;
                    }
                    else if (luisObj.topScoringIntent.actions[0].parameters[2].value[0] != null) {
                        payee = luisObj.topScoringIntent.actions[0].parameters[2].value[0].entity;
                    }

                    Activity replyWeb = activity.CreateReply($"You wish to make a payment of " + amount + " from your " + account + " account to " + payee);
                    replyWeb.Recipient = activity.From;
                    replyWeb.Type = "message";
                    replyWeb.Attachments = new List<Attachment>();

                    List<CardImage> cardImg = new List<CardImage>();
                    cardImg.Add(new CardImage(url: BANK_LOGO));

                    // ADD LOGIC TO BUTTONS
                    List<CardAction> cardAct = new List<CardAction>();
                    cardAct.Add(new CardAction() {
                        Value = "http://msa.ms",
                        Type = "openUrl",
                        Title = "Confirm",
                        Image = TICK_IMAGE
                    });
                    cardAct.Add(new CardAction() {
                        Value = "http://msa.ms",
                        Type = "openUrl",
                        Title = "Cancel",
                        Image = CROSS_IMAGE
                    });

                    HeroCard plCard = new HeroCard() {
                        Title = "Confirm Payment",
                        Subtitle = "Please confirm that you wish to make this payment",
                        Images = cardImg,
                        Buttons = cardAct
                    };

                    replyWeb.Attachments.Add(plCard.ToAttachment());
                    await connector.Conversations.ReplyToActivityAsync(replyWeb);
                    return Request.CreateResponse(HttpStatusCode.OK);

                } else if (intent == "Website") {

                    Activity replyWeb = activity.CreateReply($"Here is our website");
                    replyWeb.Recipient = activity.From;
                    replyWeb.Type = "message";
                    replyWeb.Attachments = new List<Attachment>();

                    List<CardImage> cardImg = new List<CardImage>();
                    cardImg.Add(new CardImage(url: BANK_LOGO));

                    List<CardAction> cardAct = new List<CardAction>();
                    cardAct.Add(new CardAction() {
                        Value = "http://msa.ms",
                        Type = "openUrl",
                        Title = "Open Website"
                    });

                    HeroCard plCard = new HeroCard() {
                        Title = "Contoso Website",
                        Subtitle = "Take a look at our website for even more options.",
                        Images = cardImg,
                        Buttons = cardAct
                    };

                    replyWeb.Attachments.Add(plCard.ToAttachment());
                    await connector.Conversations.ReplyToActivityAsync(replyWeb);
                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                if (output != "") {
                    Activity reply = activity.CreateReply($"" + output);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }

                return Request.CreateResponse(HttpStatusCode.OK);

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

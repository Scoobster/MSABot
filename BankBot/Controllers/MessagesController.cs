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
using Microsoft.WindowsAzure.MobileServices;
using BankBot.Controllers;
using BankBot.DataModels;

namespace BankBot
{
    //[BotAuthentication]
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

        private BankAccount ACCOUNT = null;

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity) {
            if (activity.Type == ActivityTypes.Message) {

                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                MobileServiceClient azureClient = AzureManager.AzureManagerInstance.AzureClient;


                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                LUISObject.RootObject luisObj;

                if (activity.Attachments.Count > 0) {
                
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"You sent us a photo! We will try and give you your intented action from this photo."));
                    VisionServiceClient VisionServiceClient = new VisionServiceClient("fff4c0ce2c8946a1a4a6bd8b951d13c6");
                    AnalysisResult analysisResult = await VisionServiceClient.DescribeAsync(activity.Attachments[0].ContentUrl, 3);
                    activity.Text = analysisResult.Description.Captions[0].Text;
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"The photo you sent us is of " + activity.Text));
                
                }

                HttpClient client = new HttpClient();
                string x = await client.GetStringAsync(new Uri("https://api.projectoxford.ai/luis/v2.0/apps/a4ab66bf-68bb-4e95-8951-8b179e6f3c21?subscription-key=14b2e4303e5948578989704f3ef1fd87&q=" + activity.Text + "&verbose=true"));
                luisObj = JsonConvert.DeserializeObject<LUISObject.RootObject>(x);

                string intent = luisObj.topScoringIntent.intent;

                if (activity.Text.Length > 8) {
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

                    } else if (activity.Text == "CancelPayment") {

                        await connector.Conversations.SendToConversationAsync(activity.CreateReply($"This payment has been cancelled!"));
                        return Request.CreateResponse(HttpStatusCode.OK);

                    } else {

                        if (activity.Text.Length >= 14) {
                            if (activity.Text.Substring(0, 14) == "ConfirmPayment") {

                                string[] stringParts = activity.Text.Split(' ');
                                double amount = Convert.ToDouble(stringParts[1]);
                                string payee = stringParts[2];

                                ACCOUNT = await AzureManager.AzureManagerInstance.getAccount(userData.GetProperty<string>("Name"));

                                if (ACCOUNT.Amount >= amount && await AzureManager.AzureManagerInstance.DoesExist(payee)) {

                                    ACCOUNT.Amount = ACCOUNT.Amount - amount;
                                    await AzureManager.AzureManagerInstance.UpdateAccount(ACCOUNT);

                                    BankAccount other = await AzureManager.AzureManagerInstance.getAccount(payee);
                                    other.Amount = other.Amount + amount;
                                    await AzureManager.AzureManagerInstance.UpdateAccount(other);

                                    await connector.Conversations.SendToConversationAsync(activity.CreateReply($"Payment to " + payee + " of $" + amount + " has been made."));
                                    return Request.CreateResponse(HttpStatusCode.OK);

                                }
                                else if (!(await AzureManager.AzureManagerInstance.DoesExist(payee))) {

                                    await connector.Conversations.SendToConversationAsync(activity.CreateReply($"Payee could not be found!"));
                                    return Request.CreateResponse(HttpStatusCode.NotFound);

                                }
                                else {

                                    await connector.Conversations.SendToConversationAsync(activity.CreateReply($"Payment could not be made due to insufficient funds!"));
                                    return Request.CreateResponse(HttpStatusCode.NotAcceptable);
                                }
                            }
                        }

                    }

                }

                if (!userData.GetProperty<bool>("LoggedIn") && intent == "Greeting") {
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Hello! Please login to continue..."));
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (intent == "SetName") {

                    string newName = luisObj.topScoringIntent.actions[0].parameters[0].value[0].entity;

                    ACCOUNT = await AzureManager.AzureManagerInstance.getAccount(newName);
                    if (ACCOUNT == null) {
                        await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Account Not Found"));
                        return Request.CreateResponse(HttpStatusCode.NotFound);
                    }

                    userData.SetProperty<string>("Name", " " + newName.Substring(0, 1).ToUpper() + newName.Substring(1));
                    userData.SetProperty<bool>("LoggedIn", true);
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Hello" + userData.GetProperty<string>("Name") + "!"));
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    return Request.CreateResponse(HttpStatusCode.OK);

                } else if (intent == "ClearData") {

                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    ACCOUNT = null;
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"You have been logged out"));
                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                if (!userData.GetProperty<bool>("LoggedIn")) {
                    await connector.Conversations.ReplyToActivityAsync(activity.CreateReply($"Please login!"));
                    return Request.CreateResponse(HttpStatusCode.Unauthorized);
                }

                ACCOUNT = await AzureManager.AzureManagerInstance.getAccount(userData.GetProperty<string>("Name"));

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

                    output = "You have $" + ACCOUNT.Amount + " in your account " + ACCOUNT.AccountNumber;

                } else if (intent == "MakePayment") {

                    string z = luisObj.topScoringIntent.actions[0].parameters[3].value[0].entity;
                    decimal amount = Convert.ToDecimal(z.Substring(1));

                    string payee = "undefined";
                    if (luisObj.topScoringIntent.actions[0].parameters[1].value != null) {
                        payee = luisObj.topScoringIntent.actions[0].parameters[1].value[0].entity;
                    }
                    else if (luisObj.topScoringIntent.actions[0].parameters[2].value != null) {
                        payee = luisObj.topScoringIntent.actions[0].parameters[2].value[0].entity;
                    }

                    Activity replyWeb = activity.CreateReply($"You wish to make a payment of " + amount + " to " + payee + " from " + ACCOUNT.AccountNumber);
                    replyWeb.Recipient = activity.From;
                    replyWeb.Type = "message";
                    replyWeb.Attachments = new List<Attachment>();

                    List<CardImage> cardImg = new List<CardImage>();
                    cardImg.Add(new CardImage(url: BANK_LOGO));

                    // ADD LOGIC TO BUTTONS
                    List<CardAction> cardAct = new List<CardAction>();
                    cardAct.Add(new CardAction() {
                        Value = "ConfirmPayment " + amount + " " + payee,
                        Type = "postBack",
                        Title = "Confirm",
                        Image = TICK_IMAGE
                    });
                    cardAct.Add(new CardAction() {
                        Value = "CancelPayment",
                        Type = "postBack",
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

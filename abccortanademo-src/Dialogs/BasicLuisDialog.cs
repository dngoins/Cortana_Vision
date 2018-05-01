using System;
using System.Configuration;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage;

namespace Microsoft.Bot.Sample.LuisBot
{
    // For more information about this template visit http://aka.ms/azurebots-csharp-luis
    [Serializable]
    public class BasicLuisDialog : LuisDialog<object>
    {
        public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(
            ConfigurationManager.AppSettings["LuisAppId"], 
            ConfigurationManager.AppSettings["LuisAPIKey"], 
            domain: ConfigurationManager.AppSettings["LuisAPIHostName"])))
        {
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }

        // Go to https://luis.ai and create a new intent, then train/publish your luis app.
        // Finally replace "Gretting" with the name of your newly created intent in the following handler
        [LuisIntent("Greeting")]
        public async Task GreetingIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }

        [LuisIntent("Cancel")]
        public async Task CancelIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }

        [LuisIntent("Help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            await this.ShowLuisResult(context, result);
        }


        [LuisIntent("TellMeAbout")]
        public async Task TellMeAbout(IDialogContext context, LuisResult result)
        {

            var response = context.MakeMessage();
            response.InputHint = InputHints.AcceptingInput;

            //await this.ShowLuisResult(context, result);
            var entities = result.Entities;
            if (entities.Count > 0)
            {

                //item to search for in queue
                var item = entities[0];

                var qResponse = "";

                //TODO: Get the Azure Storage Queue
                //TODO: Get the latest Item in the queue
                //TODO: Get the QueResponse from TimeSnap picture info in the queue and speak the results
                
                string sresult = qResponse;
                response.Text = sresult;
                response.Speak = sresult;
                await context.PostAsync(sresult);
                context.Wait(MessageReceived);

            }
            else
            {

                string sresult = $"I'm sorry I don't quite understand what I'm looking at.";
                response.Text = sresult;
                response.Speak = sresult;
                await context.PostAsync(sresult);
                context.Wait(MessageReceived);
            }
        }


        private async Task ShowLuisResult(IDialogContext context, LuisResult result) 
        {
            await context.PostAsync($"You have reached {result.Intents[0].Intent}. You said: {result.Query}");
            context.Wait(MessageReceived);
        }
    }
}
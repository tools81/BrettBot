using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BrettBot.Models;
using BrettBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace BrettBot.Dialogs
{
    public class BugReportDialog : ComponentDialog
    {
        private readonly StateService _stateService;

        public BugReportDialog(string dialogId, StateService stateService) : base(dialogId)
        {
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            var waterfallSteps = new WaterfallStep[]
            {
                DescriptionStepAsync,
                CallbackTimeStepAsync,
                PhoneNumberStepAsync,
                BugStepAsync,
                SummaryStepAsync
            };

            //Add named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(BugReportDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.description"));
            AddDialog(new DateTimePrompt($"{nameof(BugReportDialog)}.callbackTime", CallbackTimeValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(BugReportDialog)}.phoneNumber", PhoneNumberValidatorAsync));
            AddDialog(new ChoicePrompt($"{nameof(BugReportDialog)}.bug"));

            //Set the starting Dialog
            InitialDialogId = $"{nameof(BugReportDialog)}.mainFlow";
        }        

        private async Task<DialogTurnResult> DescriptionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.description",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Enter a description for your report")
                    }, cancellationToken);
        }

        private async Task<DialogTurnResult> CallbackTimeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["description"] = (string)stepContext.Result;

            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.callbackTime",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Please enter in a callback time"),
                        RetryPrompt = MessageFactory.Text("The value entered must be between the hours of 9 am and 5 pm.")
                    }, cancellationToken);
        }

        private async Task<DialogTurnResult> PhoneNumberStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["callbackTime"] = Convert.ToDateTime(((List<DateTimeResolution>)stepContext.Result).FirstOrDefault().Value);

            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.phoneNumber",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Please enter in a phone number that we can call you back at"),
                        RetryPrompt = MessageFactory.Text("Please enter a valid phone number")
                    }, cancellationToken);
        }

        private async Task<DialogTurnResult> BugStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["phoneNumber"] = (string)stepContext.Result;

            return await stepContext.PromptAsync($"{nameof(BugReportDialog)}.bug",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Please enter the type of bug"),
                        Choices = ChoiceFactory.ToChoices(new List<string> { "Security", "Crash", "Power", "Performance", "Usability", "Other"})
                    }, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["bug"] = ((FoundChoice)stepContext.Result).Value;

            UserProfile userProfile = await _stateService.UserProfilerAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            userProfile.Description = (string)stepContext.Values["description"];
            userProfile.CallbackTime = (DateTime)stepContext.Values["callbackTime"];
            userProfile.PhoneNumber = (string)stepContext.Values["phoneNumber"];
            userProfile.Bug = (string)stepContext.Values["bug"];

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Here is a summary of your bug report:"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Format($"Description {userProfile.Description}")), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Format($"Callback Time: {userProfile.CallbackTime.ToString()}")), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Format($"Phone Number: {userProfile.PhoneNumber}")), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Format($"Bug: {userProfile.Bug}")), cancellationToken);

            await _stateService.UserProfilerAccessor.SetAsync(stepContext.Context, userProfile);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private Task<bool> CallbackTimeValidatorAsync(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                var resolution = promptContext.Recognized.Value.First();
                DateTime selectedDate = Convert.ToDateTime(resolution.Value);
                TimeSpan start = new TimeSpan(9, 0, 0); //9 am
                TimeSpan end = new TimeSpan(17, 0, 0); //5 pm

                if ((selectedDate.TimeOfDay >= start) && (selectedDate.TimeOfDay <= end))
                {
                    valid = true;
                }
            }
            return Task.FromResult(valid);
        }

        private Task<bool> PhoneNumberValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                valid = Regex.Match(promptContext.Recognized.Value, @"^(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s-]?\d{4}$").Success;
            }
            return Task.FromResult(valid);
        }
    }
}
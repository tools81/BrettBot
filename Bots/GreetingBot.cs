using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BrettBot.Models;
using BrettBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace BrettBot.Bots
{
    public class GreetingBot : ActivityHandler
    {
        private readonly StateService _stateService;

        public GreetingBot(StateService stateService)
        {
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await GetName(turnContext, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await GetName(turnContext, cancellationToken);
                }
            }
        }

        private async Task GetName(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _stateService.UserProfilerAccessor.GetAsync(turnContext, () => new UserProfile());
            ConversationData conversationData = await _stateService.ConversationDataAccessor.GetAsync(turnContext, () => new ConversationData());

            if (!string.IsNullOrEmpty(userProfile.Name))
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"Hi {userProfile.Name}. How can I help you today?"), cancellationToken);
            }
            else
            {
                if (conversationData.PromptedUserForName)
                {
                    userProfile.Name = turnContext.Activity.Text?.Trim();
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Thanks {userProfile.Name}. How can I help you today?"), cancellationToken);
                    conversationData.PromptedUserForName = false;
                }
                else
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"What is you name?"), cancellationToken);
                    conversationData.PromptedUserForName = true;
                }

                await _stateService.UserProfilerAccessor.SetAsync(turnContext, userProfile);
                await _stateService.ConversationDataAccessor.SetAsync(turnContext, conversationData);

                await _stateService.UserState.SaveChangesAsync(turnContext);
                await _stateService.ConversationState.SaveChangesAsync(turnContext);
            }
        }
    }
}
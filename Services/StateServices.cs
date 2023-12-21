using System;
using BrettBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace BrettBot.Services
{
    public class StateService
    {
        public UserState UserState { get; }
        public ConversationState ConversationState { get; }

        public static string UserProfileId { get; } = $"{nameof(StateService)}.UserProfile";
        public static string ConversationDataId { get; } = $"{nameof(StateService)}.ConversationData";
        public static string DialogStateId { get; } = $"{nameof(StateService)}.DialogState";

        public IStatePropertyAccessor<UserProfile> UserProfilerAccessor { get; set; }
        public IStatePropertyAccessor<ConversationData> ConversationDataAccessor { get; set; }
        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }

        public StateService(UserState userState, ConversationState conversationState)
        {
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
            ConversationState = conversationState;
            InitializeAccessors();
        }

        private void InitializeAccessors()
        {
            //Initialize User State
            UserProfilerAccessor = UserState.CreateProperty<UserProfile>(UserProfileId);

            //Initialize Conversation State Accessors
            ConversationDataAccessor = ConversationState.CreateProperty<ConversationData>(ConversationDataId);
            DialogStateAccessor = ConversationState.CreateProperty<DialogState>(DialogStateId);
        }
    }
}
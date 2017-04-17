using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.Dialogs
{
    [Serializable]
    public class ConfirmModel
    {
        public bool Confirmation { get; set; }
    }


    public static class ConfirmDialog
    {
        public static string Text;

        public static IForm<ConfirmModel> BuildForm()
        {
            return new FormBuilder<ConfirmModel>()
                .Field(BuildConfirmationField())
                .Build();
        }

        private static FieldReflector<ConfirmModel> BuildConfirmationField()
        {
            var reflector = new FieldReflector<ConfirmModel>(nameof(ConfirmModel.Confirmation));
            reflector.SetType(typeof(Boolean));
            reflector.SetPrompt(new PromptAttribute(Text + "{||}"));


            return reflector;
        }
    }
}

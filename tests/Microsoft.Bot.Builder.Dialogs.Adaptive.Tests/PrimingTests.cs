﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Dialogs.Recognizers;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Bot.Builder.Dialogs.Declarative.Tests
{
    /// <summary>
    /// Test speech priming functionality.
    /// </summary>
    public class PrimingTests
    {
        private static AI.Luis.LuisAdaptiveRecognizer _luis = new AI.Luis.LuisAdaptiveRecognizer()
        {
            // The source would be generated by tooling and this is to distinguish the intent/entity across applications.
            // With package namespacing a component should name it with the package prefix, i.e. Microsoft.Welcome#foo.lu.
            PossibleIntents = new[] { new IntentDescription("intent1", "foo.lu") },
            PossibleEntities = new[] { new EntityDescription("entity1", "foo.lu"), new EntityDescription("dlist", "foo.lu") },
            DynamicLists = new[]
            {
                    new AI.Luis.DynamicList()
                    {
                        Entity = "dlist",
                        List = new List<AI.LuisV3.ListElement>
                        {
                            new AI.LuisV3.ListElement("value1", new List<string> { "synonym1", "synonym2" })
                        }
                    }
            }
        };

        private static AI.Luis.LuisAdaptiveRecognizer _luisEN = new AI.Luis.LuisAdaptiveRecognizer()
        {
            // The source would be generated by tooling and this is to distinguish the intent/entity across applications.
            // With package namespacing a component should name it with the package prefix, i.e. Microsoft.Welcome#foo.lu.
            PossibleIntents = new[] { new IntentDescription("intent1", "foo.en-us.lu") },
            PossibleEntities = new[] { new EntityDescription("entity1", "foo.en-us.lu"), new EntityDescription("dlist", "foo.en-us.lu") },
            DynamicLists = new[]
            {
                    new AI.Luis.DynamicList()
                    {
                        Entity = "dlist",
                        List = new List<AI.LuisV3.ListElement>
                        {
                            new AI.LuisV3.ListElement("value1", new List<string> { "synonym1", "synonym2" })
                        }
                    }
            }
        };

        private static AI.QnA.Recognizers.QnAMakerRecognizer _qna = new AI.QnA.Recognizers.QnAMakerRecognizer();

        private static MultiLanguageRecognizer _multi = new MultiLanguageRecognizer() { Recognizers = new Dictionary<string, Recognizer> { { "en-us", _luisEN }, { string.Empty, _luis } } };

        private static DynamicList _dlist = new DynamicList("dlist", new List<ListElement>
            {
                new ListElement("value1", new List<string> { "value1", "synonym1", "synonym2" })
            });

        private static ActivityTemplate _prompt = new ActivityTemplate("Prompt");

        private static JObject _schema = JObject.Parse(@"
            {
                'type': 'object',
                'properties': {
                    'property1': {
                        'type': 'string',
                        '$entities': ['entity1']
                    }
                }
            }");

        public static IEnumerable<object[]> ExpectedRecognizer
            => new[]
            {
                // Entity recognizers
                new object[] { new AgeEntityRecognizer(), null, new[] { new EntityDescription("age") }, null },
                new object[] { new ChannelMentionEntityRecognizer(), null, new[] { new EntityDescription("channelMention") }, null },
                new object[] { new ConfirmationEntityRecognizer(), null, new[] { new EntityDescription("boolean") }, null },
                new object[] { new CurrencyEntityRecognizer(), null, new[] { new EntityDescription("currency") }, null },
                new object[] { new DateTimeEntityRecognizer(), null, new[] { new EntityDescription("datetime") }, null },
                new object[] { new DimensionEntityRecognizer(), null, new[] { new EntityDescription("dimension") }, null },
                new object[] { new EmailEntityRecognizer(), null, new[] { new EntityDescription("email") }, null },
                new object[] { new GuidEntityRecognizer(), null, new[] { new EntityDescription("guid") }, null },
                new object[] { new HashtagEntityRecognizer(), null, new[] { new EntityDescription("hashtag") }, null },
                new object[] { new IpEntityRecognizer(), null, new[] { new EntityDescription("ip") }, null },
                new object[] { new MentionEntityRecognizer(), null, new[] { new EntityDescription("mention") }, null },
                new object[] { new NumberEntityRecognizer(), null, new[] { new EntityDescription("number") }, null },
                new object[] { new NumberRangeEntityRecognizer(), null, new[] { new EntityDescription("numberrange") }, null },
                new object[] { new OrdinalEntityRecognizer(), null, new[] { new EntityDescription("ordinal") }, null },
                new object[] { new PercentageEntityRecognizer(), null, new[] { new EntityDescription("percentage") }, null },
                new object[] { new PhoneNumberEntityRecognizer(), null, new[] { new EntityDescription("phonenumber") }, null },
                new object[] { new RegexEntityRecognizer() { Id = "somePattern", Name = "pattern" }, null, new[] { new EntityDescription("pattern", "somePattern") }, null },
                new object[] { new TemperatureEntityRecognizer(), null, new[] { new EntityDescription("temperature") }, null },
                new object[] { new UrlEntityRecognizer(), null, new[] { new EntityDescription("url") }, null },

                // LUIS
                new object[]
                {
                    _luis,
                    new[] { new IntentDescription("intent1", "foo.lu") },
                    new[] { new EntityDescription("entity1", "foo.lu"), new EntityDescription("dlist", "foo.lu") },
                    new[] { _dlist }
                },

                // QnA doesn't have any priming information
                new object[]
                {
                    _qna,
                    null, null, null
                },

                // Recognizer set
                new object[]
                {
                    new RecognizerSet() { Recognizers = new List<Recognizer> { new NumberEntityRecognizer(), _luis } },
                    new[] { new IntentDescription("intent1", "foo.lu") },
                    new[] { new EntityDescription("entity1", "foo.lu"), new EntityDescription("dlist", "foo.lu"), new EntityDescription("number") },
                    new[] { _dlist }
                },

                // Cross-trained recognizer
                new object[]
                {
                    new CrossTrainedRecognizerSet() { Recognizers = new List<Recognizer> { _luis, new NumberEntityRecognizer() } },
                    new[] { new IntentDescription("intent1", "foo.lu") },
                    new[] { new EntityDescription("entity1", "foo.lu"), new EntityDescription("dlist", "foo.lu"), new EntityDescription("number") },
                    new[] { _dlist }
                },

                // Multi language recognizer
                new object[]
                {
                    _multi,
                    new[] { new IntentDescription("intent1", "foo.lu") },
                    new[] { new EntityDescription("entity1", "foo.lu"), new EntityDescription("dlist", "foo.lu") },
                    new[] { _dlist }
                },
                new object[]
                {
                    _multi,
                    new[] { new IntentDescription("intent1", "foo.en-us.lu") },
                    new[] { new EntityDescription("entity1", "foo.en-us.lu"), new EntityDescription("dlist", "foo.en-us.lu") },
                    new[] { _dlist },
                    "en-us"
                },
            };

        public static IEnumerable<object[]> ExpectedDialog
            => new[]
            {
                new object[] { new NumberInput() { Prompt = _prompt }, null, new[] { new EntityDescription("number") }, null },
                new object[] { new ConfirmInput() { Prompt = _prompt }, null, new[] { new EntityDescription("boolean") }, null },
                new object[]
                {
                    new ChoiceInput()
                    {
                        Id = "choiceTest",
                        Prompt = _prompt,
                        Choices = new ChoiceSet(new[]
                        {
                            new Choice("value1")
                            {
                                Action = new CardAction(title: "Action"),
                                Synonyms = new List<string> { "synonym1", "synonym2" }
                            }
                        })
                    },
                    null,
                    new[] { new EntityDescription("number"), new EntityDescription("ordinal") },
                    new[] { new DynamicList("choiceTest", new List<ListElement> { new ListElement("value1", new List<string> { "value1", "Action", "synonym1", "synonym2" }) }) }
                },
                new object[]
                {
                    new ChoiceInput()
                    {
                        Id = "choiceTest",
                        Prompt = _prompt,
                        Choices = new ChoiceSet(new[]
                        {
                            new Choice("value1")
                            {
                                Action = new CardAction(title: "Action"),
                                Synonyms = new List<string> { "synonym1", "synonym2" }
                            }
                        }),
                        RecognizerOptions = new FindChoicesOptions() { NoAction = true, NoValue = true, RecognizeNumbers = false, RecognizeOrdinals = false }
                    },
                    null,
                    null,
                    new[] { new DynamicList("choiceTest", new List<ListElement> { new ListElement("value1", new List<string> { "synonym1", "synonym2" }) }) }
                },
                new object[]
                {
                    new AdaptiveDialog()
                    {
                        Recognizer = _luis,
                        Triggers = new List<Adaptive.Conditions.OnCondition>
                        {
                            new OnBeginDialog(new List<Dialog>
                            {
                                new Ask()
                                {
                                    Activity = _prompt,
                                    ExpectedProperties = new[] { "property1" }
                                }
                            })
                        },
                        Schema = _schema
                    },
                    new[] { new IntentDescription("intent1", "foo.lu") },
                    new[] { new EntityDescription("entity1", "foo.lu"), new EntityDescription("dlist", "foo.lu") },
                    new[] { _dlist },
                    null,
                    new InputContext(
                        "en-us", 
                        new RecognizerDescription(null, new[] { new EntityDescription("entity1", "foo.lu") }, null), 
                        new RecognizerDescription(
                            new[] { new IntentDescription("intent1", "foo.lu") }, 
                            new[] { new EntityDescription("entity1", "foo.lu"), new EntityDescription("dlist", "foo.lu") },
                            new[] { _dlist }))
                }
            };

        [Theory]
        [MemberData(nameof(ExpectedRecognizer))]
        public void RecognizerDescriptionTests(Recognizer recognizer, IntentDescription[] intents, EntityDescription[] entities, DynamicList[] lists, string locale = null)
        {
            CheckDescription(recognizer.GetRecognizerDescription(GetTurnContext(), locale), intents, entities, lists);
        }

        [Theory]
        [MemberData(nameof(ExpectedDialog))]
        public async Task DialogRecognizerDescriptionTests(Dialog dialog, IntentDescription[] intents, EntityDescription[] entities, DynamicList[] lists, string locale = null, InputContext input = null)
        {
            var dc = GetTurnContext(dialog);
            await dc.BeginDialogAsync(dialog.Id);
            CheckDescription(dialog.GetRecognizerDescription(dc, locale), intents, entities, lists);
            if (input == null)
            {
                CheckDescription(dc.InputContext.Possible, intents, entities, lists);
                CheckDescription(dc.InputContext.Expected, intents, entities, lists);
            }
            else
            {
                CheckDescription(dc.InputContext.Possible, input.Possible.Intents.ToArray(), input.Possible.Entities.ToArray(), input.Possible.DynamicLists.ToArray());
            }

            await dc.EndDialogAsync();
            Assert.Equal(dc.GetLocale(), dc.InputContext.Locale);
            CheckDescription(dc.InputContext.Possible, null, null, null);
            CheckDescription(dc.InputContext.Expected, null, null, null);
        }

        private void CheckDescription(RecognizerDescription description, IntentDescription[] intents, EntityDescription[] entities, DynamicList[] lists)
        {
            if (intents != null)
            {
                Assert.Equal(intents.Length, description.Intents.Count);
                Assert.All(description.Intents, (intent) => Assert.Contains(intent, intents));
            }
            else
            {
                Assert.Empty(description.Intents);
            }

            if (entities != null)
            {
                Assert.Equal(entities.Length, description.Entities.Count);
                Assert.All(description.Entities, (entity) => Assert.Contains(entity, entities));
            }
            else
            {
                Assert.Empty(description.Entities);
            }

            if (lists != null)
            {
                Assert.Equal(lists.Length, description.DynamicLists.Count);
                foreach (var entity in description.DynamicLists)
                {
                    var oracleEntity = lists.SingleOrDefault((l) => entity.Entity == l.Entity);
                    Assert.NotNull(oracleEntity);
                    Assert.Equal(oracleEntity.List.Count, entity.List.Count);
                    for (var i = 0; i < oracleEntity.List.Count; ++i)
                    {
                        var oracle = oracleEntity.List[i];
                        var element = entity.List[i];
                        Assert.Equal(oracle.CanonicalForm, element.CanonicalForm);
                        Assert.Equal(oracle.Synonyms, element.Synonyms);
                    }
                }
            }
            else
            {
                Assert.Empty(description.DynamicLists);
            }
        }

        private DialogContext GetTurnContext(Dialog dialog = null)
        {
            var dialogs = new DialogSet();
            if (dialog != null)
            {
                dialogs.Add(dialog);
            }

            var turn = new TurnContext(
                    new TestAdapter(TestAdapter.CreateConversation("Priming")),
                    new Activity());
            var dc = new DialogContext(
                dialogs,
                turn,
                new DialogState());
            dc.Services.Add<ITurnContext>(turn);
            return dc;
        }
    }
}

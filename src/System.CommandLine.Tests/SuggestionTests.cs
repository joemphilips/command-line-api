// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace System.CommandLine.Tests
{
    public class SuggestionTests
    {
        [Fact]
        public void Option_Suggest_returns_argument_suggestions_if_configured()
        {
            var option = Create.Option("--hello", "",
                                       Define.Arguments()
                                             .AddSuggestions("one", "two", "three")
                                             .ExactlyOne());

            var suggestions = option.Suggest(option.Parse("--hello"));

            suggestions.Should().BeEquivalentTo("one", "two", "three");
        }

        [Fact]
        public void Command_Suggest_returns_available_option_aliases()
        {
            var command = Create.Command("command", "a command",
                                         Create.Option("--one", "option one"),
                                         Create.Option("--two", "option two"),
                                         Create.Option("--three", "option three"));

            var suggestions = command.Suggest(command.Parse("command "));

            suggestions.Should().BeEquivalentTo("--one", "--two", "--three");
        }

        [Fact]
        public void Command_Suggest_returns_available_subcommands()
        {
            var command = Create.Command("command", "a command",
                                         Create.Command("one", "subcommand one"),
                                         Create.Command("two", "subcommand two"),
                                         Create.Command("three", "subcommand three"));

            var suggestions = command.Suggest(command.Parse("command "));

            suggestions.Should().BeEquivalentTo("one", "two", "three");
        }

        [Fact]
        public void Command_Suggest_returns_available_subcommands_and_option_aliases()
        {
            var command = Create.Command("command", "a command",
                                         Create.Command("subcommand", "subcommand"),
                                         Create.Option("--option", "option"));

            var suggestions = command.Suggest(command.Parse("command "));

            suggestions.Should().BeEquivalentTo("subcommand", "--option");
        }

        [Fact]
        public void Command_Suggest_returns_available_subcommands_and_option_aliases_and_configured_arguments()
        {
            var command = Create.Command("command", "a command",
                                         Define.Arguments()
                                               .AddSuggestions("command-argument")
                                               .OneOrMore(),
                                         Create.Command("subcommand", "subcommand"),
                                         Create.Option("--option", "option"));

            var suggestions = command.Suggest(command.Parse("command "));

            suggestions.Should()
                       .BeEquivalentTo("subcommand", "--option", "command-argument");
        }

        [Fact]
        public void An_command_can_be_hidden_from_completions_by_leaving_its_help_empty()
        {
            var command = Create.Command(
                "the-command", "Does things.",
                Create.Option("--hide-me", ""),
                Create.Option("-n", "Not hidden"));

            var suggestions = command.Parse("the-command ").Suggestions();

            suggestions.Should().NotContain("--hide-me");
        }

        [Fact]
        public void Parser_options_can_supply_context_sensitive_matches()
        {
            var parser = new OptionParser(
                Create.Option("--bread", "",
                              new ArgumentRuleBuilder()
                                  .FromAmong("wheat", "sourdough", "rye")
                                  .ExactlyOne()),
                Create.Option("--cheese", "",
                              new ArgumentRuleBuilder()
                                  .FromAmong(
                                      "provolone",
                                      "cheddar",
                                      "cream cheese")
                                  .ExactlyOne()));

            var result = parser.Parse("--bread ");

            result.Suggestions()
                  .Should()
                  .BeEquivalentTo("rye", "sourdough", "wheat");

            result = parser.Parse("--bread wheat --cheese ");

            result.Suggestions()
                  .Should()
                  .BeEquivalentTo("cheddar", "cream cheese", "provolone");
        }

        [Fact]
        public void Subcommand_names_are_available_as_suggestions()
        {
            var command = Create.Command("test", "",
                                         new ArgumentRuleBuilder().ExactlyOne(),
                                         Create.Command("one", "Command one"),
                                         Create.Command("two", "Command two"));

            command.Parse("test ")
                   .Suggestions()
                   .Should()
                   .BeEquivalentTo("one", "two");
        }

        [Fact]
        public void Both_subcommands_and_options_are_available_as_suggestions()
        {
            var command = Create.Command("test", "",
                                         new ArgumentRuleBuilder().ExactlyOne(),
                                         Create.Command("one", "Command one"),
                                         Create.Option("--one", "Option one"));

            command.Parse("test ")
                   .Suggestions()
                   .Should()
                   .BeEquivalentTo("one", "--one");
        }

        [Theory(Skip = "Needs discussion, Issue #19")]
        [InlineData("outer ")]
        [InlineData("outer -")]
        public void Option_suggestions_are_not_provided_without_matching_prefix(string input)
        {
            var parser = new CommandParser(
                Create.Command("outer", "", 
                    Create.Option("--one", "Option one"), 
                    Create.Option("--two", "Option two"), 
                    Create.Option("--three", "Option three")));

            CommandParseResult result = parser.Parse(input);
            result.Suggestions().Should().BeEmpty();
        }

        [Fact]
        public void Option_suggestions_can_be_based_on_the_proximate_option()
        {
            CommandParser parser = new CommandParser(
                Create.Command("outer", "", 
                    Create.Option("--one", "Option one"), 
                    Create.Option("--two", "Option two"), 
                    Create.Option("--three", "Option three")));

            CommandParseResult result = parser.Parse("outer ");
            result.Suggestions().Should().BeEquivalentTo("--one", "--two", "--three");
        }

        [Fact]
        public void Argument_suggestions_can_be_based_on_the_proximate_option()
        {
            var parser = new CommandParser(
                Create.Command("outer", "", 
                    Create.Option("--one", "", 
                            Define.Arguments().FromAmong("one-a", "one-b").ExactlyOne()), 
                    Create.Option("--two", "", 
                            Define.Arguments().FromAmong("two-a", "two-b").ExactlyOne())));

            CommandParseResult result = parser.Parse("outer --two ");

            result.Suggestions().Should().BeEquivalentTo("two-a", "two-b");
        }

        [Fact]
        public void Option_suggestions_can_be_based_on_the_proximate_option_and_partial_input()
        {
            var parser = new CommandParser(
                Create.Command("outer", "", 
                    Create.Command("one", "Command one"), 
                    Create.Command("two", "Command two"), 
                    Create.Command("three", "Command three")));

            CommandParseResult result = parser.Parse("outer o");

            result.Suggestions().Should().BeEquivalentTo("one", "two");
        }

        [Fact]
        public void Suggestions_can_be_provided_in_the_absence_of_validation()
        {
            Command command = Create.Command("the-command", "", Create.Option("-t", "",
                                      Define.Arguments()
                                          .AddSuggestions("vegetable",
                                              "mineral",
                                              "animal")
                                          .ExactlyOne()));

            command.Parse("the-command -t m")
                   .Suggestions()
                   .Should()
                   .BeEquivalentTo("animal",
                                   "mineral");

            command.Parse("the-command -t something-else").Errors.Should().BeEmpty();
        }

        [Fact]
        public void Suggestions_can_be_provided_using_a_delegate()
        {
            Command command = Create.Command(
                "the-command", "",
                Create.Command("one", "",
                               Define.Arguments()
                                     .AddSuggestionSource((parseResult, pos) => new[]
                                     {
                                         "vegetable",
                                         "mineral",
                                         "animal"
                                     })
                                     .ExactlyOne()));

            command.Parse("the-command one m")
                   .Suggestions()
                   .Should()
                   .BeEquivalentTo("animal",
                                   "mineral");
        }

        [Fact]
        public void When_caller_does_the_tokenizing_then_argument_suggestions_are_based_on_the_proximate_option()
        {
            var parser = new CommandParser(Create.Command("outer", "",
                    ArgumentsRule.None, Create.Option("one", "", 
                            Define.Arguments().FromAmong("one-a", "one-b", "one-c")
                                .ExactlyOne()), Create.Option("two", "", 
                            Define.Arguments().FromAmong("two-a", "two-b", "two-c")
                                .ExactlyOne()), Create.Option("three", "", 
                            Define.Arguments().FromAmong("three-a", "three-b", "three-c")
                                .ExactlyOne())));

            CommandParseResult result = parser.Parse(new[] { "outer", "two", "b" });

            result.Suggestions()
                  .Should()
                  .BeEquivalentTo("two-b");
        }

        [Fact]
        public void When_caller_does_not_do_the_tokenizing_then_argument_suggestions_are_based_on_the_proximate_option()
        {
            var parser = new CommandParser(
                Create.Command("outer", "", ArgumentsRule.None, 
                    Create.Option("one", "", 
                        Define.Arguments().FromAmong("one-a", "one-b", "one-c")
                                .ExactlyOne()), 
                    Create.Option("two", "", 
                        Define.Arguments().FromAmong("two-a", "two-b", "two-c")
                                .ExactlyOne()), 
                    Create.Option("three", "", 
                        Define.Arguments().FromAmong("three-a", "three-b", "three-c")
                                .ExactlyOne())));

            CommandParseResult result = parser.Parse("outer two b");

            result.Suggestions()
                  .Should()
                  .BeEquivalentTo("two-b");
        }

        [Fact]
        public void When_caller_does_the_tokenizing_then_argument_suggestions_are_based_on_the_proximate_command()
        {
            CommandParser parser = new CommandParser(
                Create.Command("outer", "", ArgumentsRule.None, 
                    Create.Command("one", "", 
                            Define.Arguments().FromAmong("one-a", "one-b", "one-c")
                                .ExactlyOne()), 
                    Create.Command("two", "", 
                            Define.Arguments().FromAmong("two-a", "two-b", "two-c")
                                .ExactlyOne()), 
                    Create.Command("three", "", 
                            Define.Arguments().FromAmong("three-a", "three-b", "three-c")
                                .ExactlyOne()))
                );

            CommandParseResult result = parser.Parse(new[] { "outer", "two", "b" });

            Console.WriteLine(result.Diagram());

            result.Suggestions()
                  .Should()
                  .BeEquivalentTo("two-b");
        }

        [Fact]
        public void When_caller_does_not_do_the_tokenizing_then_argument_suggestions_are_based_on_the_proximate_command()
        {
            CommandParser parser = new CommandParser(
                Create.Command("outer", "", 
                    Create.Command("one", "", 
                        Define.Arguments().FromAmong("one-a", "one-b", "one-c")
                            .ExactlyOne()), 
                    Create.Command("two", "", 
                        Define.Arguments().FromAmong("two-a", "two-b", "two-c")
                            .ExactlyOne()), 
                    Create.Command("three", "", 
                        Define.Arguments().FromAmong("three-a", "three-b", "three-c")
                            .ExactlyOne()))
            );

            CommandParseResult result = parser.Parse("outer two b");

            result.Suggestions()
                  .Should()
                  .BeEquivalentTo("two-b");
        }

        [Fact]
        public void When_position_is_unspecified_then_TextToMatch_matches_partial_argument_at_end_of_command_line()
        {
            Command command = Create.Command("the-command", "", 
                Create.Option("--option1", ""), 
                Create.Option("--option2", ""));

            string textToMatch = command.Parse("the-command t")
                                     .TextToMatch();

            textToMatch.Should().Be("t");
        }

        [Fact]
        public void When_position_is_unspecified_and_command_line_ends_with_a_space_then_TextToMatch_returns_empty()
        {
            Command command = Create.Command("the-command", "", 
                Create.Option("--option1", ""), 
                Create.Option("--option2", ""));

            string textToMatch = command.Parse("the-command t ")
                                     .TextToMatch();

            textToMatch.Should().Be("");
        }

        [Theory]
        [InlineData("the-command $one --two")]
        [InlineData("the-command one$ --two")]
        [InlineData("the-command on$e --two ")]
        [InlineData(" the-command  $one --two ")]
        [InlineData(" the-command  one$ --two ")]
        [InlineData(" the-command  on$e --two ")]
        public void When_position_is_specified_then_TextToMatch_matches_argument_at_cursor_position(string input)
        {
            Command command = Create.Command("the-command", "", Define.Arguments().ZeroOrMore());

            string textToMatch = command.Parse(input.Replace("$", ""))
                                     .TextToMatch(input.IndexOf("$", StringComparison.Ordinal));

            textToMatch.Should().Be("one");
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord.Gateway;
using Microsoft.Extensions.Logging;
using Qmmands;

namespace Disqord.Bot
{
    public abstract partial class DiscordBotBase : DiscordClientBase
    {
        private async Task MessageReceivedAsync(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message is not IGatewayUserMessage message)
                return;

            try
            {
                if (!await CheckMessageAsync(message).ConfigureAwait(false))
                    return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred while executing the check message callback.");
                return;
            }

            IEnumerable<IPrefix> prefixes;
            try
            {
                prefixes = await Prefixes.GetPrefixesAsync(message).ConfigureAwait(false);
                if (prefixes == null)
                    return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred while getting the prefixes.");
                return;
            }

            IPrefix foundPrefix = null;
            string output = null;
            try
            {
                foreach (var prefix in prefixes)
                {
                    if (prefix == null)
                        continue;

                    if (prefix.TryFind(message, out output))
                    {
                        foundPrefix = prefix;
                        break;
                    }
                }

                if (foundPrefix == null)
                    return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred while finding the prefixes in the message.");
                return;
            }

            DiscordCommandContext context;
            try
            {
                context = CreateCommandContext(foundPrefix, message, e.Channel);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred while creating the command context.");
                return;
            }

            try
            {
                if (!await BeforeExecutedAsync(context).ConfigureAwait(false))
                    return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred while executing the before executed callback.");
                return;
            }

            try
            {
                Queue.Post(output, context, static (input, context) => context.Bot.ExecuteAsync(input, context));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred while posting the execution to the command queue.");
                return;
            }
        }

        public async Task ExecuteAsync(string input, DiscordCommandContext context)
        {
            var result = await Commands.ExecuteAsync(input, context).ConfigureAwait(false);
            if (result is not FailedResult failedResult)
                return;

            // These will be handled by the CommandExecutionFailed event handler.
            if (result is ExecutionFailedResult)
                return;

            await InternalHandleFailedResultAsync(context, failedResult).ConfigureAwait(false);
        }

        private async Task DisposeContextAsync(DiscordCommandContext context)
        {
            try
            {
                await context.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred while disposing of the command context.");
            }
        }

        private async Task InternalHandleFailedResultAsync(DiscordCommandContext context, FailedResult result)
        {
            try
            {
                await HandleFailedResultAsync(context, result).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred while handling the failed result of type {0}.", result.GetType().Name);
            }

            await DisposeContextAsync(context);
        }

        private async Task CommandExecutedAsync(CommandExecutedEventArgs e)
        {
            if (e.Result is not DiscordCommandResult result)
                return;

            if (e.Context is not DiscordCommandContext context)
                return;

            try
            {
                await result.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occurred when handling command result of type {0}.", result.GetType().Name);
            }

            await DisposeContextAsync(context).ConfigureAwait(false);
        }

        private Task CommandExecutionFailedAsync(CommandExecutionFailedEventArgs e)
        {
            if (e.Result.CommandExecutionStep == CommandExecutionStep.Command && e.Result.Exception is ContextTypeMismatchException contextTypeMismatchException)
            {
                var message = "A command context type mismatch occurred while attempting to execute {0}. " +
                    "The module expected {1}, but got {2}.";
                var args = new List<object>(5)
                {
                    e.Result.Command.Name,
                    contextTypeMismatchException.ExpectedType,
                    contextTypeMismatchException.ActualType
                };

                // If the expected type is a DiscordGuildCommandContext, the actual type is a DiscordCommandContext, and the module doesn't have guild restrictions.
                if (typeof(DiscordGuildCommandContext).IsAssignableFrom(contextTypeMismatchException.ExpectedType)
                    && typeof(DiscordCommandContext).IsAssignableFrom(contextTypeMismatchException.ActualType)
                    && !CommandUtilities.EnumerateAllChecks(e.Result.Command.Module).Any(x => x is RequireGuildAttribute))
                {
                    message += " Did you forget to decorate the module with {3}?";
                    args.Add(nameof(RequireGuildAttribute));
                }

                // If the expected type is a custom made context.
                if (contextTypeMismatchException.ExpectedType != typeof(DiscordGuildCommandContext)
                    && contextTypeMismatchException.ExpectedType != typeof(DiscordCommandContext))
                {
                    message += " If you have not overridden {4}, you must do so and have it return the given context type. " +
                        "Otherwise ensure it returns the correct context types.";
                    args.Add(nameof(DiscordBotBase.CreateCommandContext));
                }

                Logger.LogError(message, args.ToArray());
            }
            else
            {
                if (e.Result.Exception is OperationCanceledException && StoppingToken.IsCancellationRequested)
                {
                    // Means the bot is stopping and any exceptions caused by cancellation we can ignore.
                    return Task.CompletedTask;
                }

                Logger.LogError(e.Result.Exception, e.Result.FailureReason);
            }

            if (e.Context is not DiscordCommandContext context)
                return Task.CompletedTask;

            return InternalHandleFailedResultAsync(context, e.Result);
        }
    }
}
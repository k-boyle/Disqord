﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Disqord.Rest;

namespace Disqord
{
    public abstract partial class CachedMessage : CachedSnowflakeEntity, IMessage
    {
        public Task AddReactionAsync(IEmoji emoji, RestRequestOptions options = null)
            => Client.AddReactionAsync(Channel.Id, Id, emoji, options);

        public Task RemoveOwnReactionAsync(IEmoji emoji, RestRequestOptions options = null)
            => Client.RemoveOwnReactionAsync(Channel.Id, Id, emoji, options);

        public Task RemoveMemberReactionAsync(Snowflake memberId, IEmoji emoji, RestRequestOptions options = null)
            => Client.RemoveMemberReactionAsync(Channel.Id, Id, memberId, emoji, options);

        public RestRequestEnumerable<RestUser> GetReactionsEnumerable(IEmoji emoji, int limit, RetrievalDirection direction = RetrievalDirection.Before, Snowflake? startFromId = null)
            => Client.GetReactionsEnumerable(Channel.Id, Id, emoji, limit, direction, startFromId);

        public Task<IReadOnlyList<RestUser>> GetReactionsAsync(IEmoji emoji, int limit = 100, RetrievalDirection direction = RetrievalDirection.Before, Snowflake? startFromId = null, RestRequestOptions options = null)
            => Client.GetReactionsAsync(Channel.Id, Id, emoji, limit, direction, startFromId, options);

        public Task ClearReactionsAsync(RestRequestOptions options = null)
            => Client.ClearReactionsAsync(Channel.Id, Id, options);

        public Task DeleteAsync(RestRequestOptions options = null)
            => Client.DeleteMessageAsync(Channel.Id, Id, options);
    }
}

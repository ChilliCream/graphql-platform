using System;
using MongoDB.Driver;

namespace MarshmallowPie.Repositories.Mongo
{
    internal static class UpdateResultExtension
    {
        public static bool IsUpserted(this UpdateResult result, Guid expectedId)
        {
            if (!result.IsAcknowledged)
            {
                return false;
            }

            if (result.ModifiedCount > 0)
            {
                return true;
            }

            return result.UpsertedId.AsNullableGuid == expectedId;
        }
    }
}

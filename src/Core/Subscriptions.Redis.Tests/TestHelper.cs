using System;
using System.Threading.Tasks;

namespace HotChocolate.Subscriptions.Redis
{
    public static class TestHelper
    {
        public static async Task TryTest(Func<Task> action)
        {
            // we will try four times ....
            int count = 0;
            int wait = 50;

            while (true)
            {
                if (count < 3)
                {
                    try
                    {
                        await action().ConfigureAwait(false);
                        break;
                    }
                    catch
                    {
                        // try again
                    }
                }
                else
                {
                    await action().ConfigureAwait(false);
                    break;
                }

                await Task.Delay(wait).ConfigureAwait(false);
                wait = wait * 2;
                count++;
            }
        }
    }
}

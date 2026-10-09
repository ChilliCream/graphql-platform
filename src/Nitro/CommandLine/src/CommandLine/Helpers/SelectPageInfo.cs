namespace ChilliCream.Nitro.CommandLine.Helpers;

internal delegate IPaginationPageInfo? SelectPageInfo<in TResult>(TResult result);

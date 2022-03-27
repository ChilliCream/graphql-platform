namespace HotChocolate.Language;

internal static partial class GraphQLConstants
{
    static GraphQLConstants()
    {
        InitializeIsLetterOrUnderscoreCache();
        InitializeIsLetterOrDigitOUnderscoreCache();
        InitializeIsDigitCache();
        InitializeIsDigitOrMinusCache();
        InitializeTrimComment();
    }

    private static void InitializeIsLetterOrUnderscoreCache()
    {
        for (var c = 'a'; c <= 'z'; c++)
        {
            _isLetterOrUnderscore[c] = true;
        }

        for (var c = 'A'; c <= 'Z'; c++)
        {
            _isLetterOrUnderscore[c] = true;
        }

        _isLetterOrUnderscore['_'] = true;
    }

    private static void InitializeIsLetterOrDigitOUnderscoreCache()
    {
        for (var c = 'a'; c <= 'z'; c++)
        {
            _isLetterOrDigitOrUnderscore[c] = true;
        }

        for (var c = 'A'; c <= 'Z'; c++)
        {
            _isLetterOrDigitOrUnderscore[c] = true;
        }

        _isLetterOrDigitOrUnderscore['0'] = true;
        _isLetterOrDigitOrUnderscore['1'] = true;
        _isLetterOrDigitOrUnderscore['2'] = true;
        _isLetterOrDigitOrUnderscore['3'] = true;
        _isLetterOrDigitOrUnderscore['4'] = true;
        _isLetterOrDigitOrUnderscore['5'] = true;
        _isLetterOrDigitOrUnderscore['6'] = true;
        _isLetterOrDigitOrUnderscore['7'] = true;
        _isLetterOrDigitOrUnderscore['8'] = true;
        _isLetterOrDigitOrUnderscore['9'] = true;

        _isLetterOrDigitOrUnderscore['_'] = true;
    }

    private static void InitializeIsDigitOrMinusCache()
    {
        _isDigitOrMinus['-'] = true;
        _isDigitOrMinus['0'] = true;
        _isDigitOrMinus['1'] = true;
        _isDigitOrMinus['2'] = true;
        _isDigitOrMinus['3'] = true;
        _isDigitOrMinus['4'] = true;
        _isDigitOrMinus['5'] = true;
        _isDigitOrMinus['6'] = true;
        _isDigitOrMinus['7'] = true;
        _isDigitOrMinus['8'] = true;
        _isDigitOrMinus['9'] = true;
    }

    private static void InitializeIsDigitCache()
    {
        _isDigit['0'] = true;
        _isDigit['1'] = true;
        _isDigit['2'] = true;
        _isDigit['3'] = true;
        _isDigit['4'] = true;
        _isDigit['5'] = true;
        _isDigit['6'] = true;
        _isDigit['7'] = true;
        _isDigit['8'] = true;
        _isDigit['9'] = true;
    }

    private static void InitializeTrimComment()
    {
        _trimComment[Hash] = true;
        _trimComment[Space] = true;
        _trimComment[HorizontalTab] = true;
    }
}

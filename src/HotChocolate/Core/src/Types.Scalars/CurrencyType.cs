using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    public class CurrencyType : StringType
    {

        private static readonly string _validationPattern =
            ScalarResources.CurrencyType_ValidationPattern;

        private static readonly string _suffixPattern =
            ScalarResources.CurrencyType_SuffixPattern;

        private static readonly string _prefixPattern =
            ScalarResources.CurrencyType_PrefixPattern;

        private static readonly Regex _suffixRegex =
            new(_suffixPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _prefixRegex =
            new(_prefixPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);


        private static readonly Tuple<string, int, Tuple<DecimalSeparatorKind, string>> _defaultTuple
                = new Tuple<string, int, Tuple<DecimalSeparatorKind, string>>
                   ("", 0, new Tuple<DecimalSeparatorKind, string>(DecimalSeparatorKind.Undefined, ""));

        public CurrencyType() :
            this(WellKnownScalarTypes.Currency,
                 ScalarResources.CurrencyType_Description)
        {
        }

        public CurrencyType(
                NameString name,
                string? description = null,
                BindingBehavior bind = BindingBehavior.Explicit) :
            base(name, description, bind)
        {
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(string runtimeValue)
        {
            return IsMatching(runtimeValue);
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        {
            return IsMatching(valueSyntax.Value);
        }

        /// <inheritdoc />
        protected override string ParseLiteral(StringValueNode valueSyntax)
        {
            if (!IsMatching(valueSyntax.Value))
            {
                throw ThrowHelper.CurrencyType_ParseLiteral_IsInvalid(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(string runtimeValue)
        {
           
            if (!IsMatching(runtimeValue))
            {
                throw ThrowHelper.CurrencyType_ParseValue_IsInvalid(this);
            }

            return base.ParseValue(runtimeValue);
        }

        /// <inheritdoc />
        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is string s &&
               IsMatching(s))
            {
                resultValue = s;
                return true;
            }

            resultValue = null;
            return false;
        }

        /// <inheritdoc />
        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is string s &&
               IsMatching(s))
            {
                runtimeValue = s;
                return true;
            }

            runtimeValue = null;
            return false;
        }

        public static ResourceSet GetEnumResourceValues()
        {
#pragma warning disable CS8603 // Possible null reference return.
            return CurrencyCodes.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
#pragma warning restore CS8603 // Possible null reference return.
        }

        public static Tuple<DecimalSeparatorKind, string> GetDecimalSeparatorKind(string? cultureInfoCodes)
        {
           
            DecimalSeparatorKind decimalKind = DecimalSeparatorKind.Undefined;
            var exampleSep = ".";
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var cultureCodes = cultureInfoCodes.Split('|');
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            if (cultureCodes.Length > 1)
            {
                NumberFormatInfo nfi = new CultureInfo(cultureCodes[1], false).NumberFormat;
                exampleSep = nfi.NumberDecimalSeparator;

                if (cultureCodes.Length.Equals(2))
                {
                    decimalKind = DecimalSeparatorKind.Default;
                }
                else
                {
                    foreach (var cc in cultureCodes.Select(x => x).Where(x => !x.Equals(cultureCodes.First())))
                    {
                        if (!exampleSep.Equals(new CultureInfo(cc, false).NumberFormat.NumberDecimalSeparator))
                        {
                            decimalKind = DecimalSeparatorKind.Mixed;
                            break;
                        }
                    }
                }
            }

            return new Tuple<DecimalSeparatorKind, string>(decimalKind, exampleSep);
        }

        public static bool IsMatching(string str)
        {
            var match = false;

            Match matchPre = _prefixRegex.Match(str);
            Match matchSuf = _suffixRegex.Match(str);
            Match containLetters = new Regex(@"[A-Z]").Match(str);

            IEnumerable<DictionaryEntry> rep = GetEnumResourceValues().Cast<DictionaryEntry>();
            Tuple<string, int, Tuple<DecimalSeparatorKind, string>> regexTuple = _defaultTuple;

            if (containLetters.Success)
            {
                if (matchPre.Success)
                {
                    regexTuple = GetRegexTuple(matchPre, rep, true, true);
                }
                else if (matchSuf.Success)
                {
                    regexTuple = GetRegexTuple(matchSuf, rep, true, false);
                }
            }
            else
            {
                if (matchPre.Success)
                {
                    regexTuple = GetRegexTuple(matchPre, rep, false, true);
                }
            }


            if (!regexTuple.Equals(_defaultTuple))
            {
                switch (regexTuple.Item3.Item1)
                {
                    case DecimalSeparatorKind.Undefined:
                        {
                            GetMatching(str, regexTuple, ",", @"\.", out match);
                        }
                        break;
                    case DecimalSeparatorKind.Default:
                        {
                            var _separator = regexTuple.Item3.Item2.Equals(".") ? @"\." : regexTuple.Item3.Item2;
                            var _3digit = regexTuple.Item3.Item2.Equals(".") ? "," : @"\.";
                            GetMatching(str, regexTuple, _3digit, _separator, out match);
                        }
                        break;
                    case DecimalSeparatorKind.Mixed:
                        {
                            GetMatching(str, regexTuple, ",", @"\.", out match);

                            if (!match)
                            {
                                GetMatching(str, regexTuple, @"\.", ",", out match);
                            }
                        }
                        break;
                }
            }

            return match;
        }

        private static void GetMatching(string str, Tuple<string, int, Tuple<DecimalSeparatorKind, string>> regexTuple, string _d, string _sep, out bool match)
        {
            Regex regPattern = new Regex(_validationPattern.Replace("<block>", regexTuple.Item1)
                                                          .Replace("<digit>", regexTuple.Item2.ToString())
                                                          .Replace("<point>", regexTuple.Item2.Equals(0) ? "?" : "")
                                                          .Replace("<3digit>", _d)
                                                          .Replace("<separator>", _sep));
            match = regPattern.IsMatch(str);
        }

        private static Tuple<string, int, Tuple<DecimalSeparatorKind, string>> GetRegexTuple(Match match, IEnumerable<DictionaryEntry> rep, bool alpha, bool prefix)
        {
            Tuple<string, int, Tuple<DecimalSeparatorKind, string>> strs = _defaultTuple;
            var _strs = rep.ToList()
                  .Select(x =>
                      new
                      {
                          code = alpha ?
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                    x.Key.ToString().Split('|').ToList().First()

                                    : x.Key.ToString().Split('|').ToList().Last(),
                          digit = int.Parse(x.Value.ToString().Split('|').First()),
                          separatorKind = GetDecimalSeparatorKind(x.Value.ToString())
                      })
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                  .Where(x => x.code.ToString().Contains(
                                    (prefix ?
                                        match.Value.Substring(0, 3)
                                        : match.Value.Substring(1, 3))))
                  .ToList()
                  .FirstOrDefault();

            if (_strs != null)
                strs = new Tuple<string, int, Tuple<DecimalSeparatorKind, string>>
                                    (_strs.code, _strs.digit, _strs.separatorKind);
            return strs;
        }

        public enum DecimalSeparatorKind
        {
            Default,
            Mixed,
            Undefined
        }
    }
}

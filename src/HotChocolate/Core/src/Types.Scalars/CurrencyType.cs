using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Language;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `Currency` scalar type represent a valid Currency as described on ISO-4217, represented as a UTF-8
    /// character sequence. The scalar follows the specification defined by the
    /// <a href="https://en.wikipedia.org/wiki/ISO_4217">
    /// HTML Spec
    /// </a>
    /// </summary>
    public class CurrencyType : StringType
    {
        private const int _defaultRegexTimeoutInMs = 100;

        private readonly Dictionary<Tuple<string, string>, List<string>> _dic =
            new Dictionary<Tuple<string, string>, List<string>>
            {
                { new Tuple<string, string> (".", "2"),
                    new List<string>{ "036", "044", "050", "051", "052", "060", "064", "072", "084", "090", "104", "116", "124", "136", "144", "156", "192", "203", "208", "214", "222", "230", "232", "238", "242", "292", "320", "328", "340", "344", "356", "364", "376", "388", "404", "408", "418", "422", "430", "446", "454", "458", "462", "480", "484", "496", "516", "524", "532", "554", "558", "566", "586", "590", "598", "604", "608", "634", "654", "682", "702", "706", "728", "748", "760", "764", "776", "780", "784", "818", "826", "834", "882", "886", "901", "931", "932", "936", "938", "951", "967", "969", "976", "979", "997", "AED", "AMD", "ANG", "AUD", "BBD", "BDT", "BMD", "BSD", "BTN", "BWP", "BZD", "CAD", "CDF", "CNY", "CUC", "CUP", "CZK", "DKK", "DOP", "EGP", "ERN", "ETB", "FJD", "FKP", "GBP", "GHS", "GIP", "GTQ", "GYD", "HKD", "HNL", "ILS", "INR", "IRR", "JMD", "KES", "KHR", "KPW", "KYD", "LAK", "LBP", "LKR", "LRD", "MGA", "MMK", "MNT", "MOP", "MUR", "MVR", "MWK", "MXN", "MXV", "MYR", "NAD", "NGN", "NIO", "NPR", "NZD", "PAB", "PEN", "PGK", "PHP", "PKR", "QAR", "SAR", "SBD", "SDG", "SGD", "SHP", "SOS", "SSP", "SVC", "SYP", "SZL", "THB", "TOP", "TTD", "TWD", "TZS", "USN", "WST", "XCD", "YER", "ZMW", "ZWL"}
                },
                { new Tuple<string, string> (",", "2"),
                    new List<string>{ "008", "012", "032", "068", "096", "132", "170", "188", "191", "270", "332", "348", "360", "398", "417", "426", "498", "504", "533", "578", "643", "690", "694", "752", "807", "858", "860", "928", "929", "930", "933", "934", "941", "943", "944", "946", "947", "948", "949", "968", "970", "971", "972", "973", "975", "977", "980", "981", "984", "985", "986", "AFN", "ALL", "AOA", "ARS", "AWG", "AZN", "BAM", "BGN", "BND", "BOB", "BOV", "BRL", "BYN", "CHE", "CHW", "COP", "COU", "CRC", "CVE", "DZD", "GEL", "GMD", "HRK", "HTG", "HUF", "IDR", "KGS", "KZT", "LSL", "MAD", "MDL", "MKD", "MRU", "MZN", "NOK", "PLN", "RON", "RSD", "RUB", "SCR", "SEK", "SLL", "SRD", "STN", "TJS", "TMT", "TRY", "UAH", "UYU", "UZS", "VES"}
                },
                { new Tuple<string, string> ("", "0"),
                    new List<string>{ "108", "152", "174", "262", "324", "352", "392", "410", "548", "600", "646", "704", "800", "940", "950", "952", "953", "955", "956", "957", "958", "959", "960", "961", "962", "963", "964", "965", "994", "999", "BIF", "CLP", "DJF", "GNF", "ISK", "JPY", "KMF", "KRW", "PYG", "RWF", "UGX", "UYI", "VND", "VUV", "XAF", "XAG", "XAU", "XBA", "XBB", "XBC", "XBD", "XDR", "XOF", "XPD", "XPF", "XPT", "XSU", "XTS", "XUA", "XXX"}
                },
                { new Tuple<string, string> (".", "3"),
                    new List<string>{ "048", "368", "400", "414", "434", "512", "BHD", "IQD", "JOD", "KWD", "LYD", "OMR"}
                },
                { new Tuple<string, string> (",", "3"),
                    new List<string>{ "TND", "788"}
                },
                { new Tuple<string, string> (",.", "2"),
                    new List<string>{ "710", "756", "840", "978", "CHF", "EUR", "USD", "ZAR"}
                },
                { new Tuple<string, string> (",", "4"),
                    new List<string>{ "927", "990", "CLF", "UYW"}
                }
            };


        private const string _validationPattern = "^(((?:(<block>)\u00A0)?" +
             "((([-]?\\d{1,3})" +
             "(?(?!(<separator>\\d{3}))((<3digit>\\d{3})*(<separator>(?<nd>)\\d{<digit>}(?<-nd>))<point>)|" +
             "(?(<mixed>)((<separator>\\d{3})*)(<3digit>(?<nd>)\\d{<digit>}(?<-nd>))<point>)))|" +
             "([-]?\\d{1,19}(<separator_n>(?<nd>)\\d{<digit>}(?<-nd>))<point>))?)|" +
             "(((([-]?\\d{1,3})" +
             "(?(?!(<separator>\\d{3}))((<3digit>\\d{3})*(<separator>(?<nd>)\\d{<digit>}(?<-nd>))<point>)|" +
             "(?(<mixed>)((<separator>\\d{3})*)(<3digit>(?<nd>)\\d{<digit>}(?<-nd>))<point>)))|" +
             "([-]?\\d{1,19}(<separator_n>(?<nd>)\\d{<digit>}(?<-nd>))<point>))" +
             "(?:\u00A0(<block>))?))(?(nd)(?!))$";

        private const string _suffixPattern =
            @"\u00A0<pat>{3}$";

        private const string _prefixPattern =
            @"^<pat>{3}\u00A0";

        private readonly Regex _suffixRegex =
            new Regex(_suffixPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex _prefixRegex =
            new Regex(_prefixPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);



        /// <summary>
        /// Initializes a new instance of the <see cref="CurrencyType"/> class.
        /// </summary>
        public CurrencyType()
            : this(
                WellKnownScalarTypes.Currency,
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
        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is string s
                && IsMatching(s))
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

            if (resultValue is string s
                && IsMatching(s))
            {
                runtimeValue = s;
                return true;
            }

            runtimeValue = null;
            return false;
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
       
        private bool IsMatching(string runtimeValue)
        {
            Regex validationRegex;
            GetRegex(runtimeValue, out validationRegex);
            return validationRegex.IsMatch(runtimeValue);
        }

        private void GetRegex(string str, out Regex validationRegex)
        {
            Match matchPre = _prefixRegex.Match(str);
            Match matchSuf = _suffixRegex.Match(str);
            Match containLetters = new Regex(@"[A-Z]").Match(str);

            string pattern = "";

            string _block = "", _digit = "", _point = "", _tdigit = "", _separator = "", _mixed = "?!", _separator_n = "";
            bool foundMatch = false;

            if (containLetters.Success)
            {
                matchPre = new Regex(_prefixPattern.Replace("<pat>", "[A-Z]")).Match(str);
                matchSuf = new Regex(_suffixPattern.Replace("<pat>", "[A-Z]")).Match(str);

                if (matchPre.Success)
                {
                    foundMatch = GetPatterns(matchPre.Value.Substring(0, 3), out _block, out _digit,
                        out _point, out _tdigit, out _separator, out _separator_n, out _mixed);
                }
                else if (matchSuf.Success)
                {
                    foundMatch = GetPatterns(matchSuf.Value.Substring(1, 3), out _block, out _digit,
                        out _point, out _tdigit, out _separator, out _separator_n, out _mixed);
                }
            }
            else
            {
                matchPre = new Regex(_prefixPattern.Replace("<pat>", "\\d")).Match(str);

                if (matchPre.Success)
                {
                    foundMatch = GetPatterns(matchPre.Value.Substring(0, 3), out _block, out _digit,
                        out _point, out _tdigit, out _separator, out _separator_n, out _mixed);
                }
            }

            if (!foundMatch)
            {
                pattern = @"\A(?!x)x";
            }
            else
            {
                pattern = _validationPattern.Replace("<block>", _block)
                                                          .Replace("<digit>", _digit)
                                                          .Replace("<point>", _point)
                                                          .Replace("<3digit>", _tdigit)
                                                          .Replace("<separator>", _separator)
                                                          .Replace("<separator_n>", _separator_n)
                                                          .Replace("<mixed>", _mixed);
            }

            validationRegex = new Regex(
                    pattern,
                    RegexOptions.Compiled,
                    TimeSpan.FromMilliseconds(_defaultRegexTimeoutInMs));

        }

        private bool GetPatterns(string matchValue, out string block, out string digit, out string point,
            out string tdigit, out string separator, out string separator_n, out string mixed)
        {
            block = digit = point = tdigit = separator = mixed = separator_n = "";
            bool vars = false;
            foreach (Tuple<string, string> key in _dic.Keys)
            {
                var value = new List<string> { };
                if (_dic.TryGetValue(key, out value))
                {
                    if (value.Contains(matchValue))
                    {
                        block = matchValue;
                        digit = key.Item2;
                        point = digit.Equals(0) ? "?" : "";
                        tdigit = key.Item1.Equals(",") ? "\\." : (key.Item1.Equals("\\.") ? "," : "(,|\\.)");
                        separator = key.Item1.Equals(",.") ? "." : key.Item1;
                        separator_n = key.Item1.Equals(",.") ? "(,|\\.)" : key.Item1;
                        mixed = key.Item1.Equals(",.") ? "?=" : "?!";
                        vars = true;
                        break;
                    }
                }
            }

            return vars;
        }
    }
}

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The pool of first names <see cref="AgentActorAllocator"/> draws from when it mints a new
/// agent name. Every entry satisfies the <c>MailAgentName</c> rules and none collides with a
/// well-known agent role word.
/// </summary>
internal static class AgentNamePool
{
    private static readonly string[] s_names =
    [
        "ackbar", "ahsoka", "aladdin", "albus", "alice", "anakin", "andy", "angel", "anna", "annabelle",
        "annabeth", "aragorn", "argus", "ariel", "arwen", "ash", "atta", "atticus", "austin", "bail",
        "balin", "bard", "barry", "bathilda", "baymax", "bella", "bellatrix", "belle", "benjamin", "betsy",
        "biff", "bilbo", "billy", "bishop", "blondie", "boba", "bobby", "boromir", "brand", "brian",
        "bruce", "bubba", "bucky", "butch", "buzz", "cal", "cameron", "carl", "carlito", "carol",
        "carrie", "cassian", "cedric", "charles", "chewbacca", "cho", "chucky", "cinna", "claire", "clara",
        "clarice", "clark", "clayton", "clint", "commodus", "connie", "coraline", "coriolanus", "dallas", "dan",
        "danny", "denethor", "diana", "django", "dobby", "dolores", "dominic", "donny", "dooku", "dorothy",
        "dory", "draco", "drax", "dutch", "dwalin", "edward", "effie", "einstein", "elinor", "elle",
        "ellen", "ellie", "elliot", "elrond", "elsa", "elvira", "emmett", "eowyn", "erik", "esmeralda",
        "ethan", "eve", "faramir", "fergus", "ferris", "fili", "filius", "finn", "finnick", "fiona",
        "fleur", "flik", "forrest", "fred", "fredo", "frenchy", "frodo", "frollo", "furiosa", "fury",
        "galadriel", "gale", "gamora", "gandalf", "gaston", "george", "gertie", "gimli", "ginny", "gisele",
        "glinda", "gogo", "gollum", "groot", "grover", "hagrid", "hamm", "hammond", "han", "hank",
        "hannibal", "harley", "harry", "haymitch", "hector", "hedwig", "hela", "henry", "hercules", "hermione",
        "hicks", "hiro", "holdo", "hope", "hux", "ilsa", "indiana", "iorek", "iris", "jabba",
        "jack", "jacob", "jafar", "jane", "jango", "jasmine", "jason", "jasper", "jean", "jeffrey",
        "jem", "jennifer", "jenny", "jessie", "jimmy", "johanna", "john", "josey", "joy", "jules",
        "jyn", "kamala", "kane", "karen", "katarina", "katniss", "kenickie", "kida", "kili", "kingsley",
        "kitty", "korg", "kristoff", "kubo", "kurt", "kuzco", "kyle", "kylo", "lando", "laura",
        "laurie", "legolas", "leia", "letty", "lex", "lilo", "logan", "loki", "lorraine", "louis",
        "lucilla", "lucius", "luke", "luna", "lyra", "malcolm", "manny", "mara", "marion", "marla",
        "marlin", "marsellus", "marty", "maude", "maui", "max", "maximus", "megara", "megatron", "merida",
        "merry", "mia", "miguel", "mike", "mikey", "milo", "minerva", "moana", "molly", "monica",
        "morpheus", "mufasa", "mulan", "muldoon", "myrtle", "nakia", "nala", "nani", "narcissa", "natasha",
        "nedry", "nemo", "neo", "neville", "newt", "nick", "nien", "norman", "nova", "nux",
        "nymphadora", "obi", "okoye", "olaf", "optimus", "ororo", "oswald", "owen", "pacha", "padme",
        "palpatine", "pamela", "paulie", "peeta", "pennywise", "pepper", "percy", "peter", "phil", "piotr",
        "pippin", "plo", "pocahontas", "poe", "pomona", "primrose", "pumbaa", "quasimodo", "quill", "radagast",
        "rambo", "raven", "regan", "remus", "remy", "renesmee", "rex", "rexy", "rey", "rhett",
        "rick", "riley", "ripley", "riri", "rizzo", "robert", "roman", "ron", "rosalie", "rose",
        "rubeus", "russell", "sam", "samara", "sandy", "sarah", "saruman", "scabior", "scarlett", "scott",
        "scout", "sebastian", "selina", "severus", "shmi", "shrek", "shuri", "simba", "sirius", "sloane",
        "smaug", "smith", "snoke", "sonny", "stephen", "steve", "sulley", "tarzan", "tej", "theoden",
        "theodore", "thor", "thorin", "thranduil", "thrawn", "timon", "tom", "tommy", "tony", "toto",
        "travis", "treebeard", "trinity", "tuco", "tyler", "ursula", "valkyrie", "victor", "viktor", "vincent",
        "vito", "wade", "walter", "wanda", "warren", "watto", "william", "willie", "wong", "woody",
        "wybie", "yoda", "yondu"
    ];

    /// <summary>
    /// The full name pool, in declaration order.
    /// </summary>
    internal static IReadOnlyList<string> Names => s_names;
}

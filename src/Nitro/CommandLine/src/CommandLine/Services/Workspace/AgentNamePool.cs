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
        "biff", "bilbo", "billy", "bishop", "blondie", "blue", "boba", "bobby", "boo", "boromir",
        "brand", "brian", "bruce", "bubba", "bucky", "bumblebee", "butch", "buzz", "cal", "cameron",
        "carl", "carlito", "carol", "carrie", "cassian", "cedric", "charles", "chewbacca", "cho", "chucky",
        "chunk", "cinna", "claire", "clara", "clarice", "clark", "clayton", "clint", "commodus", "connie",
        "coraline", "coriolanus", "dallas", "dan", "danny", "data", "denethor", "diana", "django", "dobby",
        "doc", "dolores", "dominic", "donkey", "donny", "dooku", "dorothy", "dory", "draco", "drax",
        "dutch", "dwalin", "edward", "effie", "einstein", "elinor", "elle", "ellen", "ellie", "elliot",
        "elrond", "elsa", "elvira", "emmett", "eowyn", "erik", "esmeralda", "ethan", "eve", "faramir",
        "fergus", "ferris", "fili", "filius", "finn", "finnick", "fiona", "fleur", "flik", "forrest",
        "fred", "fredo", "frenchy", "frodo", "frollo", "furiosa", "fury", "galadriel", "gale", "gamora",
        "gandalf", "gaston", "genie", "george", "gertie", "gimli", "ginny", "gisele", "glinda", "gogo",
        "gollum", "grievous", "groot", "grover", "hagrid", "hamm", "hammond", "han", "hank", "hannibal",
        "happy", "harley", "harry", "haymitch", "hector", "hedwig", "hela", "henry", "hercules", "hermione",
        "hicks", "hiro", "holdo", "hope", "hux", "ilsa", "indiana", "iorek", "iris", "jabba",
        "jack", "jacob", "jafar", "jane", "jango", "jasmine", "jason", "jasper", "jean", "jeffrey",
        "jem", "jennifer", "jenny", "jessie", "jimmy", "johanna", "john", "josey", "joy", "jules",
        "jyn", "kamala", "kane", "karen", "katarina", "katniss", "kenickie", "kida", "kili", "kingsley",
        "kitty", "korg", "kristoff", "kubo", "kurt", "kuzco", "kyle", "kylo", "lando", "laura",
        "laurie", "legolas", "leia", "letty", "lex", "lightning", "lilo", "linguini", "logan", "loki",
        "lorraine", "louis", "lucilla", "lucius", "luke", "luna", "lyra", "mace", "malcolm", "manny",
        "mantis", "mara", "marion", "marla", "marlin", "marsellus", "marty", "mater", "maude", "maui",
        "maul", "max", "maximus", "megara", "megatron", "merida", "merry", "mia", "michael", "miguel",
        "mike", "mikey", "milo", "minerva", "moana", "molly", "monica", "morpheus", "mufasa", "mulan",
        "muldoon", "myrtle", "nakia", "nala", "nani", "narcissa", "natasha", "nebula", "nedry", "nemo",
        "neo", "neville", "newt", "nick", "nien", "norman", "nova", "nux", "nymphadora", "obi",
        "okoye", "olaf", "optimus", "ororo", "oswald", "owen", "pacha", "padme", "palpatine", "pamela",
        "paulie", "peeta", "pennywise", "pepper", "percy", "peter", "phil", "piotr", "pippin", "plo",
        "pocahontas", "poe", "pomona", "primrose", "pumbaa", "puss", "quasimodo", "quill", "radagast", "rambo",
        "raven", "regan", "remus", "remy", "renesmee", "rex", "rexy", "rey", "rhett", "rick",
        "riley", "ripley", "riri", "rizzo", "robert", "rocket", "roman", "ron", "rooster", "rosalie",
        "rose", "rubeus", "russell", "sadness", "sam", "samara", "sandy", "sarah", "saruman", "scabior",
        "scar", "scarlett", "scott", "scout", "sebastian", "selina", "severus", "shmi", "shrek", "shuri",
        "simba", "sirius", "sloane", "sloth", "smaug", "smith", "snoke", "sonny", "stephen", "steve",
        "stitch", "sulley", "tarzan", "tej", "theoden", "theodore", "thor", "thorin", "thranduil", "thrawn",
        "timon", "tom", "tommy", "tony", "toto", "travis", "treebeard", "trinity", "tuco", "tyler",
        "ursula", "valkyrie", "victor", "viktor", "vincent", "vision", "vito", "wade", "wall", "walter",
        "wanda", "warren", "watto", "wedge", "william", "willie", "wong", "woody", "wybie", "yoda",
        "yondu"
    ];

    /// <summary>
    /// The full name pool, in declaration order.
    /// </summary>
    internal static IReadOnlyList<string> Names => s_names;
}

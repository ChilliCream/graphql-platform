# Paging_Empty_PagingArgs

## SQL 0

```sql
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
ORDER BY b."Name", b."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.QueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id)
```

## Result 3

```json
{
  "HasNextPage": false,
  "HasPreviousPage": false,
  "First": 1,
  "FirstCursor": "QnJhbmRcOjA6MQ==",
  "Last": 100,
  "LastCursor": "QnJhbmRcOjk5OjEwMA=="
}
```

## Result 4

```json
[
  {
    "Id": 1,
    "Name": "Brand:0",
    "DisplayName": "BrandDisplay0",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country0"
      }
    }
  },
  {
    "Id": 2,
    "Name": "Brand:1",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country1"
      }
    }
  },
  {
    "Id": 11,
    "Name": "Brand:10",
    "DisplayName": "BrandDisplay10",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country10"
      }
    }
  },
  {
    "Id": 12,
    "Name": "Brand:11",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country11"
      }
    }
  },
  {
    "Id": 13,
    "Name": "Brand:12",
    "DisplayName": "BrandDisplay12",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country12"
      }
    }
  },
  {
    "Id": 14,
    "Name": "Brand:13",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country13"
      }
    }
  },
  {
    "Id": 15,
    "Name": "Brand:14",
    "DisplayName": "BrandDisplay14",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country14"
      }
    }
  },
  {
    "Id": 16,
    "Name": "Brand:15",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country15"
      }
    }
  },
  {
    "Id": 17,
    "Name": "Brand:16",
    "DisplayName": "BrandDisplay16",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country16"
      }
    }
  },
  {
    "Id": 18,
    "Name": "Brand:17",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country17"
      }
    }
  },
  {
    "Id": 19,
    "Name": "Brand:18",
    "DisplayName": "BrandDisplay18",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country18"
      }
    }
  },
  {
    "Id": 20,
    "Name": "Brand:19",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country19"
      }
    }
  },
  {
    "Id": 3,
    "Name": "Brand:2",
    "DisplayName": "BrandDisplay2",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country2"
      }
    }
  },
  {
    "Id": 21,
    "Name": "Brand:20",
    "DisplayName": "BrandDisplay20",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country20"
      }
    }
  },
  {
    "Id": 22,
    "Name": "Brand:21",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country21"
      }
    }
  },
  {
    "Id": 23,
    "Name": "Brand:22",
    "DisplayName": "BrandDisplay22",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country22"
      }
    }
  },
  {
    "Id": 24,
    "Name": "Brand:23",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country23"
      }
    }
  },
  {
    "Id": 25,
    "Name": "Brand:24",
    "DisplayName": "BrandDisplay24",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country24"
      }
    }
  },
  {
    "Id": 26,
    "Name": "Brand:25",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country25"
      }
    }
  },
  {
    "Id": 27,
    "Name": "Brand:26",
    "DisplayName": "BrandDisplay26",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country26"
      }
    }
  },
  {
    "Id": 28,
    "Name": "Brand:27",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country27"
      }
    }
  },
  {
    "Id": 29,
    "Name": "Brand:28",
    "DisplayName": "BrandDisplay28",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country28"
      }
    }
  },
  {
    "Id": 30,
    "Name": "Brand:29",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country29"
      }
    }
  },
  {
    "Id": 4,
    "Name": "Brand:3",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country3"
      }
    }
  },
  {
    "Id": 31,
    "Name": "Brand:30",
    "DisplayName": "BrandDisplay30",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country30"
      }
    }
  },
  {
    "Id": 32,
    "Name": "Brand:31",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country31"
      }
    }
  },
  {
    "Id": 33,
    "Name": "Brand:32",
    "DisplayName": "BrandDisplay32",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country32"
      }
    }
  },
  {
    "Id": 34,
    "Name": "Brand:33",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country33"
      }
    }
  },
  {
    "Id": 35,
    "Name": "Brand:34",
    "DisplayName": "BrandDisplay34",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country34"
      }
    }
  },
  {
    "Id": 36,
    "Name": "Brand:35",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country35"
      }
    }
  },
  {
    "Id": 37,
    "Name": "Brand:36",
    "DisplayName": "BrandDisplay36",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country36"
      }
    }
  },
  {
    "Id": 38,
    "Name": "Brand:37",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country37"
      }
    }
  },
  {
    "Id": 39,
    "Name": "Brand:38",
    "DisplayName": "BrandDisplay38",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country38"
      }
    }
  },
  {
    "Id": 40,
    "Name": "Brand:39",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country39"
      }
    }
  },
  {
    "Id": 5,
    "Name": "Brand:4",
    "DisplayName": "BrandDisplay4",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country4"
      }
    }
  },
  {
    "Id": 41,
    "Name": "Brand:40",
    "DisplayName": "BrandDisplay40",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country40"
      }
    }
  },
  {
    "Id": 42,
    "Name": "Brand:41",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country41"
      }
    }
  },
  {
    "Id": 43,
    "Name": "Brand:42",
    "DisplayName": "BrandDisplay42",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country42"
      }
    }
  },
  {
    "Id": 44,
    "Name": "Brand:43",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country43"
      }
    }
  },
  {
    "Id": 45,
    "Name": "Brand:44",
    "DisplayName": "BrandDisplay44",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country44"
      }
    }
  },
  {
    "Id": 46,
    "Name": "Brand:45",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country45"
      }
    }
  },
  {
    "Id": 47,
    "Name": "Brand:46",
    "DisplayName": "BrandDisplay46",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country46"
      }
    }
  },
  {
    "Id": 48,
    "Name": "Brand:47",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country47"
      }
    }
  },
  {
    "Id": 49,
    "Name": "Brand:48",
    "DisplayName": "BrandDisplay48",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country48"
      }
    }
  },
  {
    "Id": 50,
    "Name": "Brand:49",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country49"
      }
    }
  },
  {
    "Id": 6,
    "Name": "Brand:5",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country5"
      }
    }
  },
  {
    "Id": 51,
    "Name": "Brand:50",
    "DisplayName": "BrandDisplay50",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country50"
      }
    }
  },
  {
    "Id": 52,
    "Name": "Brand:51",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country51"
      }
    }
  },
  {
    "Id": 53,
    "Name": "Brand:52",
    "DisplayName": "BrandDisplay52",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country52"
      }
    }
  },
  {
    "Id": 54,
    "Name": "Brand:53",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country53"
      }
    }
  },
  {
    "Id": 55,
    "Name": "Brand:54",
    "DisplayName": "BrandDisplay54",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country54"
      }
    }
  },
  {
    "Id": 56,
    "Name": "Brand:55",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country55"
      }
    }
  },
  {
    "Id": 57,
    "Name": "Brand:56",
    "DisplayName": "BrandDisplay56",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country56"
      }
    }
  },
  {
    "Id": 58,
    "Name": "Brand:57",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country57"
      }
    }
  },
  {
    "Id": 59,
    "Name": "Brand:58",
    "DisplayName": "BrandDisplay58",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country58"
      }
    }
  },
  {
    "Id": 60,
    "Name": "Brand:59",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country59"
      }
    }
  },
  {
    "Id": 7,
    "Name": "Brand:6",
    "DisplayName": "BrandDisplay6",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country6"
      }
    }
  },
  {
    "Id": 61,
    "Name": "Brand:60",
    "DisplayName": "BrandDisplay60",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country60"
      }
    }
  },
  {
    "Id": 62,
    "Name": "Brand:61",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country61"
      }
    }
  },
  {
    "Id": 63,
    "Name": "Brand:62",
    "DisplayName": "BrandDisplay62",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country62"
      }
    }
  },
  {
    "Id": 64,
    "Name": "Brand:63",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country63"
      }
    }
  },
  {
    "Id": 65,
    "Name": "Brand:64",
    "DisplayName": "BrandDisplay64",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country64"
      }
    }
  },
  {
    "Id": 66,
    "Name": "Brand:65",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country65"
      }
    }
  },
  {
    "Id": 67,
    "Name": "Brand:66",
    "DisplayName": "BrandDisplay66",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country66"
      }
    }
  },
  {
    "Id": 68,
    "Name": "Brand:67",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country67"
      }
    }
  },
  {
    "Id": 69,
    "Name": "Brand:68",
    "DisplayName": "BrandDisplay68",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country68"
      }
    }
  },
  {
    "Id": 70,
    "Name": "Brand:69",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country69"
      }
    }
  },
  {
    "Id": 8,
    "Name": "Brand:7",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country7"
      }
    }
  },
  {
    "Id": 71,
    "Name": "Brand:70",
    "DisplayName": "BrandDisplay70",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country70"
      }
    }
  },
  {
    "Id": 72,
    "Name": "Brand:71",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country71"
      }
    }
  },
  {
    "Id": 73,
    "Name": "Brand:72",
    "DisplayName": "BrandDisplay72",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country72"
      }
    }
  },
  {
    "Id": 74,
    "Name": "Brand:73",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country73"
      }
    }
  },
  {
    "Id": 75,
    "Name": "Brand:74",
    "DisplayName": "BrandDisplay74",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country74"
      }
    }
  },
  {
    "Id": 76,
    "Name": "Brand:75",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country75"
      }
    }
  },
  {
    "Id": 77,
    "Name": "Brand:76",
    "DisplayName": "BrandDisplay76",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country76"
      }
    }
  },
  {
    "Id": 78,
    "Name": "Brand:77",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country77"
      }
    }
  },
  {
    "Id": 79,
    "Name": "Brand:78",
    "DisplayName": "BrandDisplay78",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country78"
      }
    }
  },
  {
    "Id": 80,
    "Name": "Brand:79",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country79"
      }
    }
  },
  {
    "Id": 9,
    "Name": "Brand:8",
    "DisplayName": "BrandDisplay8",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country8"
      }
    }
  },
  {
    "Id": 81,
    "Name": "Brand:80",
    "DisplayName": "BrandDisplay80",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country80"
      }
    }
  },
  {
    "Id": 82,
    "Name": "Brand:81",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country81"
      }
    }
  },
  {
    "Id": 83,
    "Name": "Brand:82",
    "DisplayName": "BrandDisplay82",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country82"
      }
    }
  },
  {
    "Id": 84,
    "Name": "Brand:83",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country83"
      }
    }
  },
  {
    "Id": 85,
    "Name": "Brand:84",
    "DisplayName": "BrandDisplay84",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country84"
      }
    }
  },
  {
    "Id": 86,
    "Name": "Brand:85",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country85"
      }
    }
  },
  {
    "Id": 87,
    "Name": "Brand:86",
    "DisplayName": "BrandDisplay86",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country86"
      }
    }
  },
  {
    "Id": 88,
    "Name": "Brand:87",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country87"
      }
    }
  },
  {
    "Id": 89,
    "Name": "Brand:88",
    "DisplayName": "BrandDisplay88",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country88"
      }
    }
  },
  {
    "Id": 90,
    "Name": "Brand:89",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country89"
      }
    }
  },
  {
    "Id": 10,
    "Name": "Brand:9",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country9"
      }
    }
  },
  {
    "Id": 91,
    "Name": "Brand:90",
    "DisplayName": "BrandDisplay90",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country90"
      }
    }
  },
  {
    "Id": 92,
    "Name": "Brand:91",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country91"
      }
    }
  },
  {
    "Id": 93,
    "Name": "Brand:92",
    "DisplayName": "BrandDisplay92",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country92"
      }
    }
  },
  {
    "Id": 94,
    "Name": "Brand:93",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country93"
      }
    }
  },
  {
    "Id": 95,
    "Name": "Brand:94",
    "DisplayName": "BrandDisplay94",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country94"
      }
    }
  },
  {
    "Id": 96,
    "Name": "Brand:95",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country95"
      }
    }
  },
  {
    "Id": 97,
    "Name": "Brand:96",
    "DisplayName": "BrandDisplay96",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country96"
      }
    }
  },
  {
    "Id": 98,
    "Name": "Brand:97",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country97"
      }
    }
  },
  {
    "Id": 99,
    "Name": "Brand:98",
    "DisplayName": "BrandDisplay98",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country98"
      }
    }
  },
  {
    "Id": 100,
    "Name": "Brand:99",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country99"
      }
    }
  }
]
```


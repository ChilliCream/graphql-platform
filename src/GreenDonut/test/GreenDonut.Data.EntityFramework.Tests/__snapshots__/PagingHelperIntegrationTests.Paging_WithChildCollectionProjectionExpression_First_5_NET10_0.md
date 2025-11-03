# Paging_WithChildCollectionProjectionExpression_First_5

## SQL 0

```sql
-- @p='6'
SELECT b0."Id", b0."Name", p."Id", p."Name"
FROM (
    SELECT b."Id", b."Name"
    FROM "Brands" AS b
    ORDER BY b."Name", b."Id"
    LIMIT @p
) AS b0
LEFT JOIN "Products" AS p ON b0."Id" = p."BrandId"
ORDER BY b0."Name", b0."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Select(brand => new BrandWithProductsDto() {Id = brand.Id, Name = brand.Name, Products = brand.Products.AsQueryable().Select(ProductDto.Projection).ToList()}).OrderBy(t => t.Name).ThenBy(t => t.Id).Take(6)
```

## Result 3

```json
{
  "HasNextPage": true,
  "HasPreviousPage": false,
  "First": 1,
  "FirstCursor": "e31CcmFuZFw6MDox",
  "Last": 13,
  "LastCursor": "e31CcmFuZFw6MTI6MTM="
}
```

## Result 4

```json
[
  {
    "Id": 1,
    "Name": "Brand:0",
    "Products": [
      {
        "Id": 1,
        "Name": "Product 0-0"
      },
      {
        "Id": 2,
        "Name": "Product 0-1"
      },
      {
        "Id": 3,
        "Name": "Product 0-2"
      },
      {
        "Id": 4,
        "Name": "Product 0-3"
      },
      {
        "Id": 5,
        "Name": "Product 0-4"
      },
      {
        "Id": 6,
        "Name": "Product 0-5"
      },
      {
        "Id": 7,
        "Name": "Product 0-6"
      },
      {
        "Id": 8,
        "Name": "Product 0-7"
      },
      {
        "Id": 9,
        "Name": "Product 0-8"
      },
      {
        "Id": 10,
        "Name": "Product 0-9"
      },
      {
        "Id": 11,
        "Name": "Product 0-10"
      },
      {
        "Id": 12,
        "Name": "Product 0-11"
      },
      {
        "Id": 13,
        "Name": "Product 0-12"
      },
      {
        "Id": 14,
        "Name": "Product 0-13"
      },
      {
        "Id": 15,
        "Name": "Product 0-14"
      },
      {
        "Id": 16,
        "Name": "Product 0-15"
      },
      {
        "Id": 17,
        "Name": "Product 0-16"
      },
      {
        "Id": 18,
        "Name": "Product 0-17"
      },
      {
        "Id": 19,
        "Name": "Product 0-18"
      },
      {
        "Id": 20,
        "Name": "Product 0-19"
      },
      {
        "Id": 21,
        "Name": "Product 0-20"
      },
      {
        "Id": 22,
        "Name": "Product 0-21"
      },
      {
        "Id": 23,
        "Name": "Product 0-22"
      },
      {
        "Id": 24,
        "Name": "Product 0-23"
      },
      {
        "Id": 25,
        "Name": "Product 0-24"
      },
      {
        "Id": 26,
        "Name": "Product 0-25"
      },
      {
        "Id": 27,
        "Name": "Product 0-26"
      },
      {
        "Id": 28,
        "Name": "Product 0-27"
      },
      {
        "Id": 29,
        "Name": "Product 0-28"
      },
      {
        "Id": 30,
        "Name": "Product 0-29"
      },
      {
        "Id": 31,
        "Name": "Product 0-30"
      },
      {
        "Id": 32,
        "Name": "Product 0-31"
      },
      {
        "Id": 33,
        "Name": "Product 0-32"
      },
      {
        "Id": 34,
        "Name": "Product 0-33"
      },
      {
        "Id": 35,
        "Name": "Product 0-34"
      },
      {
        "Id": 36,
        "Name": "Product 0-35"
      },
      {
        "Id": 37,
        "Name": "Product 0-36"
      },
      {
        "Id": 38,
        "Name": "Product 0-37"
      },
      {
        "Id": 39,
        "Name": "Product 0-38"
      },
      {
        "Id": 40,
        "Name": "Product 0-39"
      },
      {
        "Id": 41,
        "Name": "Product 0-40"
      },
      {
        "Id": 42,
        "Name": "Product 0-41"
      },
      {
        "Id": 43,
        "Name": "Product 0-42"
      },
      {
        "Id": 44,
        "Name": "Product 0-43"
      },
      {
        "Id": 45,
        "Name": "Product 0-44"
      },
      {
        "Id": 46,
        "Name": "Product 0-45"
      },
      {
        "Id": 47,
        "Name": "Product 0-46"
      },
      {
        "Id": 48,
        "Name": "Product 0-47"
      },
      {
        "Id": 49,
        "Name": "Product 0-48"
      },
      {
        "Id": 50,
        "Name": "Product 0-49"
      },
      {
        "Id": 51,
        "Name": "Product 0-50"
      },
      {
        "Id": 52,
        "Name": "Product 0-51"
      },
      {
        "Id": 53,
        "Name": "Product 0-52"
      },
      {
        "Id": 54,
        "Name": "Product 0-53"
      },
      {
        "Id": 55,
        "Name": "Product 0-54"
      },
      {
        "Id": 56,
        "Name": "Product 0-55"
      },
      {
        "Id": 57,
        "Name": "Product 0-56"
      },
      {
        "Id": 58,
        "Name": "Product 0-57"
      },
      {
        "Id": 59,
        "Name": "Product 0-58"
      },
      {
        "Id": 60,
        "Name": "Product 0-59"
      },
      {
        "Id": 61,
        "Name": "Product 0-60"
      },
      {
        "Id": 62,
        "Name": "Product 0-61"
      },
      {
        "Id": 63,
        "Name": "Product 0-62"
      },
      {
        "Id": 64,
        "Name": "Product 0-63"
      },
      {
        "Id": 65,
        "Name": "Product 0-64"
      },
      {
        "Id": 66,
        "Name": "Product 0-65"
      },
      {
        "Id": 67,
        "Name": "Product 0-66"
      },
      {
        "Id": 68,
        "Name": "Product 0-67"
      },
      {
        "Id": 69,
        "Name": "Product 0-68"
      },
      {
        "Id": 70,
        "Name": "Product 0-69"
      },
      {
        "Id": 71,
        "Name": "Product 0-70"
      },
      {
        "Id": 72,
        "Name": "Product 0-71"
      },
      {
        "Id": 73,
        "Name": "Product 0-72"
      },
      {
        "Id": 74,
        "Name": "Product 0-73"
      },
      {
        "Id": 75,
        "Name": "Product 0-74"
      },
      {
        "Id": 76,
        "Name": "Product 0-75"
      },
      {
        "Id": 77,
        "Name": "Product 0-76"
      },
      {
        "Id": 78,
        "Name": "Product 0-77"
      },
      {
        "Id": 79,
        "Name": "Product 0-78"
      },
      {
        "Id": 80,
        "Name": "Product 0-79"
      },
      {
        "Id": 81,
        "Name": "Product 0-80"
      },
      {
        "Id": 82,
        "Name": "Product 0-81"
      },
      {
        "Id": 83,
        "Name": "Product 0-82"
      },
      {
        "Id": 84,
        "Name": "Product 0-83"
      },
      {
        "Id": 85,
        "Name": "Product 0-84"
      },
      {
        "Id": 86,
        "Name": "Product 0-85"
      },
      {
        "Id": 87,
        "Name": "Product 0-86"
      },
      {
        "Id": 88,
        "Name": "Product 0-87"
      },
      {
        "Id": 89,
        "Name": "Product 0-88"
      },
      {
        "Id": 90,
        "Name": "Product 0-89"
      },
      {
        "Id": 91,
        "Name": "Product 0-90"
      },
      {
        "Id": 92,
        "Name": "Product 0-91"
      },
      {
        "Id": 93,
        "Name": "Product 0-92"
      },
      {
        "Id": 94,
        "Name": "Product 0-93"
      },
      {
        "Id": 95,
        "Name": "Product 0-94"
      },
      {
        "Id": 96,
        "Name": "Product 0-95"
      },
      {
        "Id": 97,
        "Name": "Product 0-96"
      },
      {
        "Id": 98,
        "Name": "Product 0-97"
      },
      {
        "Id": 99,
        "Name": "Product 0-98"
      },
      {
        "Id": 100,
        "Name": "Product 0-99"
      }
    ]
  },
  {
    "Id": 2,
    "Name": "Brand:1",
    "Products": [
      {
        "Id": 101,
        "Name": "Product 1-0"
      },
      {
        "Id": 102,
        "Name": "Product 1-1"
      },
      {
        "Id": 103,
        "Name": "Product 1-2"
      },
      {
        "Id": 104,
        "Name": "Product 1-3"
      },
      {
        "Id": 105,
        "Name": "Product 1-4"
      },
      {
        "Id": 106,
        "Name": "Product 1-5"
      },
      {
        "Id": 107,
        "Name": "Product 1-6"
      },
      {
        "Id": 108,
        "Name": "Product 1-7"
      },
      {
        "Id": 109,
        "Name": "Product 1-8"
      },
      {
        "Id": 110,
        "Name": "Product 1-9"
      },
      {
        "Id": 111,
        "Name": "Product 1-10"
      },
      {
        "Id": 112,
        "Name": "Product 1-11"
      },
      {
        "Id": 113,
        "Name": "Product 1-12"
      },
      {
        "Id": 114,
        "Name": "Product 1-13"
      },
      {
        "Id": 115,
        "Name": "Product 1-14"
      },
      {
        "Id": 116,
        "Name": "Product 1-15"
      },
      {
        "Id": 117,
        "Name": "Product 1-16"
      },
      {
        "Id": 118,
        "Name": "Product 1-17"
      },
      {
        "Id": 119,
        "Name": "Product 1-18"
      },
      {
        "Id": 120,
        "Name": "Product 1-19"
      },
      {
        "Id": 121,
        "Name": "Product 1-20"
      },
      {
        "Id": 122,
        "Name": "Product 1-21"
      },
      {
        "Id": 123,
        "Name": "Product 1-22"
      },
      {
        "Id": 124,
        "Name": "Product 1-23"
      },
      {
        "Id": 125,
        "Name": "Product 1-24"
      },
      {
        "Id": 126,
        "Name": "Product 1-25"
      },
      {
        "Id": 127,
        "Name": "Product 1-26"
      },
      {
        "Id": 128,
        "Name": "Product 1-27"
      },
      {
        "Id": 129,
        "Name": "Product 1-28"
      },
      {
        "Id": 130,
        "Name": "Product 1-29"
      },
      {
        "Id": 131,
        "Name": "Product 1-30"
      },
      {
        "Id": 132,
        "Name": "Product 1-31"
      },
      {
        "Id": 133,
        "Name": "Product 1-32"
      },
      {
        "Id": 134,
        "Name": "Product 1-33"
      },
      {
        "Id": 135,
        "Name": "Product 1-34"
      },
      {
        "Id": 136,
        "Name": "Product 1-35"
      },
      {
        "Id": 137,
        "Name": "Product 1-36"
      },
      {
        "Id": 138,
        "Name": "Product 1-37"
      },
      {
        "Id": 139,
        "Name": "Product 1-38"
      },
      {
        "Id": 140,
        "Name": "Product 1-39"
      },
      {
        "Id": 141,
        "Name": "Product 1-40"
      },
      {
        "Id": 142,
        "Name": "Product 1-41"
      },
      {
        "Id": 143,
        "Name": "Product 1-42"
      },
      {
        "Id": 144,
        "Name": "Product 1-43"
      },
      {
        "Id": 145,
        "Name": "Product 1-44"
      },
      {
        "Id": 146,
        "Name": "Product 1-45"
      },
      {
        "Id": 147,
        "Name": "Product 1-46"
      },
      {
        "Id": 148,
        "Name": "Product 1-47"
      },
      {
        "Id": 149,
        "Name": "Product 1-48"
      },
      {
        "Id": 150,
        "Name": "Product 1-49"
      },
      {
        "Id": 151,
        "Name": "Product 1-50"
      },
      {
        "Id": 152,
        "Name": "Product 1-51"
      },
      {
        "Id": 153,
        "Name": "Product 1-52"
      },
      {
        "Id": 154,
        "Name": "Product 1-53"
      },
      {
        "Id": 155,
        "Name": "Product 1-54"
      },
      {
        "Id": 156,
        "Name": "Product 1-55"
      },
      {
        "Id": 157,
        "Name": "Product 1-56"
      },
      {
        "Id": 158,
        "Name": "Product 1-57"
      },
      {
        "Id": 159,
        "Name": "Product 1-58"
      },
      {
        "Id": 160,
        "Name": "Product 1-59"
      },
      {
        "Id": 161,
        "Name": "Product 1-60"
      },
      {
        "Id": 162,
        "Name": "Product 1-61"
      },
      {
        "Id": 163,
        "Name": "Product 1-62"
      },
      {
        "Id": 164,
        "Name": "Product 1-63"
      },
      {
        "Id": 165,
        "Name": "Product 1-64"
      },
      {
        "Id": 166,
        "Name": "Product 1-65"
      },
      {
        "Id": 167,
        "Name": "Product 1-66"
      },
      {
        "Id": 168,
        "Name": "Product 1-67"
      },
      {
        "Id": 169,
        "Name": "Product 1-68"
      },
      {
        "Id": 170,
        "Name": "Product 1-69"
      },
      {
        "Id": 171,
        "Name": "Product 1-70"
      },
      {
        "Id": 172,
        "Name": "Product 1-71"
      },
      {
        "Id": 173,
        "Name": "Product 1-72"
      },
      {
        "Id": 174,
        "Name": "Product 1-73"
      },
      {
        "Id": 175,
        "Name": "Product 1-74"
      },
      {
        "Id": 176,
        "Name": "Product 1-75"
      },
      {
        "Id": 177,
        "Name": "Product 1-76"
      },
      {
        "Id": 178,
        "Name": "Product 1-77"
      },
      {
        "Id": 179,
        "Name": "Product 1-78"
      },
      {
        "Id": 180,
        "Name": "Product 1-79"
      },
      {
        "Id": 181,
        "Name": "Product 1-80"
      },
      {
        "Id": 182,
        "Name": "Product 1-81"
      },
      {
        "Id": 183,
        "Name": "Product 1-82"
      },
      {
        "Id": 184,
        "Name": "Product 1-83"
      },
      {
        "Id": 185,
        "Name": "Product 1-84"
      },
      {
        "Id": 186,
        "Name": "Product 1-85"
      },
      {
        "Id": 187,
        "Name": "Product 1-86"
      },
      {
        "Id": 188,
        "Name": "Product 1-87"
      },
      {
        "Id": 189,
        "Name": "Product 1-88"
      },
      {
        "Id": 190,
        "Name": "Product 1-89"
      },
      {
        "Id": 191,
        "Name": "Product 1-90"
      },
      {
        "Id": 192,
        "Name": "Product 1-91"
      },
      {
        "Id": 193,
        "Name": "Product 1-92"
      },
      {
        "Id": 194,
        "Name": "Product 1-93"
      },
      {
        "Id": 195,
        "Name": "Product 1-94"
      },
      {
        "Id": 196,
        "Name": "Product 1-95"
      },
      {
        "Id": 197,
        "Name": "Product 1-96"
      },
      {
        "Id": 198,
        "Name": "Product 1-97"
      },
      {
        "Id": 199,
        "Name": "Product 1-98"
      },
      {
        "Id": 200,
        "Name": "Product 1-99"
      }
    ]
  },
  {
    "Id": 11,
    "Name": "Brand:10",
    "Products": [
      {
        "Id": 1001,
        "Name": "Product 10-0"
      },
      {
        "Id": 1002,
        "Name": "Product 10-1"
      },
      {
        "Id": 1003,
        "Name": "Product 10-2"
      },
      {
        "Id": 1004,
        "Name": "Product 10-3"
      },
      {
        "Id": 1005,
        "Name": "Product 10-4"
      },
      {
        "Id": 1006,
        "Name": "Product 10-5"
      },
      {
        "Id": 1007,
        "Name": "Product 10-6"
      },
      {
        "Id": 1008,
        "Name": "Product 10-7"
      },
      {
        "Id": 1009,
        "Name": "Product 10-8"
      },
      {
        "Id": 1010,
        "Name": "Product 10-9"
      },
      {
        "Id": 1011,
        "Name": "Product 10-10"
      },
      {
        "Id": 1012,
        "Name": "Product 10-11"
      },
      {
        "Id": 1013,
        "Name": "Product 10-12"
      },
      {
        "Id": 1014,
        "Name": "Product 10-13"
      },
      {
        "Id": 1015,
        "Name": "Product 10-14"
      },
      {
        "Id": 1016,
        "Name": "Product 10-15"
      },
      {
        "Id": 1017,
        "Name": "Product 10-16"
      },
      {
        "Id": 1018,
        "Name": "Product 10-17"
      },
      {
        "Id": 1019,
        "Name": "Product 10-18"
      },
      {
        "Id": 1020,
        "Name": "Product 10-19"
      },
      {
        "Id": 1021,
        "Name": "Product 10-20"
      },
      {
        "Id": 1022,
        "Name": "Product 10-21"
      },
      {
        "Id": 1023,
        "Name": "Product 10-22"
      },
      {
        "Id": 1024,
        "Name": "Product 10-23"
      },
      {
        "Id": 1025,
        "Name": "Product 10-24"
      },
      {
        "Id": 1026,
        "Name": "Product 10-25"
      },
      {
        "Id": 1027,
        "Name": "Product 10-26"
      },
      {
        "Id": 1028,
        "Name": "Product 10-27"
      },
      {
        "Id": 1029,
        "Name": "Product 10-28"
      },
      {
        "Id": 1030,
        "Name": "Product 10-29"
      },
      {
        "Id": 1031,
        "Name": "Product 10-30"
      },
      {
        "Id": 1032,
        "Name": "Product 10-31"
      },
      {
        "Id": 1033,
        "Name": "Product 10-32"
      },
      {
        "Id": 1034,
        "Name": "Product 10-33"
      },
      {
        "Id": 1035,
        "Name": "Product 10-34"
      },
      {
        "Id": 1036,
        "Name": "Product 10-35"
      },
      {
        "Id": 1037,
        "Name": "Product 10-36"
      },
      {
        "Id": 1038,
        "Name": "Product 10-37"
      },
      {
        "Id": 1039,
        "Name": "Product 10-38"
      },
      {
        "Id": 1040,
        "Name": "Product 10-39"
      },
      {
        "Id": 1041,
        "Name": "Product 10-40"
      },
      {
        "Id": 1042,
        "Name": "Product 10-41"
      },
      {
        "Id": 1043,
        "Name": "Product 10-42"
      },
      {
        "Id": 1044,
        "Name": "Product 10-43"
      },
      {
        "Id": 1045,
        "Name": "Product 10-44"
      },
      {
        "Id": 1046,
        "Name": "Product 10-45"
      },
      {
        "Id": 1047,
        "Name": "Product 10-46"
      },
      {
        "Id": 1048,
        "Name": "Product 10-47"
      },
      {
        "Id": 1049,
        "Name": "Product 10-48"
      },
      {
        "Id": 1050,
        "Name": "Product 10-49"
      },
      {
        "Id": 1051,
        "Name": "Product 10-50"
      },
      {
        "Id": 1052,
        "Name": "Product 10-51"
      },
      {
        "Id": 1053,
        "Name": "Product 10-52"
      },
      {
        "Id": 1054,
        "Name": "Product 10-53"
      },
      {
        "Id": 1055,
        "Name": "Product 10-54"
      },
      {
        "Id": 1056,
        "Name": "Product 10-55"
      },
      {
        "Id": 1057,
        "Name": "Product 10-56"
      },
      {
        "Id": 1058,
        "Name": "Product 10-57"
      },
      {
        "Id": 1059,
        "Name": "Product 10-58"
      },
      {
        "Id": 1060,
        "Name": "Product 10-59"
      },
      {
        "Id": 1061,
        "Name": "Product 10-60"
      },
      {
        "Id": 1062,
        "Name": "Product 10-61"
      },
      {
        "Id": 1063,
        "Name": "Product 10-62"
      },
      {
        "Id": 1064,
        "Name": "Product 10-63"
      },
      {
        "Id": 1065,
        "Name": "Product 10-64"
      },
      {
        "Id": 1066,
        "Name": "Product 10-65"
      },
      {
        "Id": 1067,
        "Name": "Product 10-66"
      },
      {
        "Id": 1068,
        "Name": "Product 10-67"
      },
      {
        "Id": 1069,
        "Name": "Product 10-68"
      },
      {
        "Id": 1070,
        "Name": "Product 10-69"
      },
      {
        "Id": 1071,
        "Name": "Product 10-70"
      },
      {
        "Id": 1072,
        "Name": "Product 10-71"
      },
      {
        "Id": 1073,
        "Name": "Product 10-72"
      },
      {
        "Id": 1074,
        "Name": "Product 10-73"
      },
      {
        "Id": 1075,
        "Name": "Product 10-74"
      },
      {
        "Id": 1076,
        "Name": "Product 10-75"
      },
      {
        "Id": 1077,
        "Name": "Product 10-76"
      },
      {
        "Id": 1078,
        "Name": "Product 10-77"
      },
      {
        "Id": 1079,
        "Name": "Product 10-78"
      },
      {
        "Id": 1080,
        "Name": "Product 10-79"
      },
      {
        "Id": 1081,
        "Name": "Product 10-80"
      },
      {
        "Id": 1082,
        "Name": "Product 10-81"
      },
      {
        "Id": 1083,
        "Name": "Product 10-82"
      },
      {
        "Id": 1084,
        "Name": "Product 10-83"
      },
      {
        "Id": 1085,
        "Name": "Product 10-84"
      },
      {
        "Id": 1086,
        "Name": "Product 10-85"
      },
      {
        "Id": 1087,
        "Name": "Product 10-86"
      },
      {
        "Id": 1088,
        "Name": "Product 10-87"
      },
      {
        "Id": 1089,
        "Name": "Product 10-88"
      },
      {
        "Id": 1090,
        "Name": "Product 10-89"
      },
      {
        "Id": 1091,
        "Name": "Product 10-90"
      },
      {
        "Id": 1092,
        "Name": "Product 10-91"
      },
      {
        "Id": 1093,
        "Name": "Product 10-92"
      },
      {
        "Id": 1094,
        "Name": "Product 10-93"
      },
      {
        "Id": 1095,
        "Name": "Product 10-94"
      },
      {
        "Id": 1096,
        "Name": "Product 10-95"
      },
      {
        "Id": 1097,
        "Name": "Product 10-96"
      },
      {
        "Id": 1098,
        "Name": "Product 10-97"
      },
      {
        "Id": 1099,
        "Name": "Product 10-98"
      },
      {
        "Id": 1100,
        "Name": "Product 10-99"
      }
    ]
  },
  {
    "Id": 12,
    "Name": "Brand:11",
    "Products": [
      {
        "Id": 1101,
        "Name": "Product 11-0"
      },
      {
        "Id": 1102,
        "Name": "Product 11-1"
      },
      {
        "Id": 1103,
        "Name": "Product 11-2"
      },
      {
        "Id": 1104,
        "Name": "Product 11-3"
      },
      {
        "Id": 1105,
        "Name": "Product 11-4"
      },
      {
        "Id": 1106,
        "Name": "Product 11-5"
      },
      {
        "Id": 1107,
        "Name": "Product 11-6"
      },
      {
        "Id": 1108,
        "Name": "Product 11-7"
      },
      {
        "Id": 1109,
        "Name": "Product 11-8"
      },
      {
        "Id": 1110,
        "Name": "Product 11-9"
      },
      {
        "Id": 1111,
        "Name": "Product 11-10"
      },
      {
        "Id": 1112,
        "Name": "Product 11-11"
      },
      {
        "Id": 1113,
        "Name": "Product 11-12"
      },
      {
        "Id": 1114,
        "Name": "Product 11-13"
      },
      {
        "Id": 1115,
        "Name": "Product 11-14"
      },
      {
        "Id": 1116,
        "Name": "Product 11-15"
      },
      {
        "Id": 1117,
        "Name": "Product 11-16"
      },
      {
        "Id": 1118,
        "Name": "Product 11-17"
      },
      {
        "Id": 1119,
        "Name": "Product 11-18"
      },
      {
        "Id": 1120,
        "Name": "Product 11-19"
      },
      {
        "Id": 1121,
        "Name": "Product 11-20"
      },
      {
        "Id": 1122,
        "Name": "Product 11-21"
      },
      {
        "Id": 1123,
        "Name": "Product 11-22"
      },
      {
        "Id": 1124,
        "Name": "Product 11-23"
      },
      {
        "Id": 1125,
        "Name": "Product 11-24"
      },
      {
        "Id": 1126,
        "Name": "Product 11-25"
      },
      {
        "Id": 1127,
        "Name": "Product 11-26"
      },
      {
        "Id": 1128,
        "Name": "Product 11-27"
      },
      {
        "Id": 1129,
        "Name": "Product 11-28"
      },
      {
        "Id": 1130,
        "Name": "Product 11-29"
      },
      {
        "Id": 1131,
        "Name": "Product 11-30"
      },
      {
        "Id": 1132,
        "Name": "Product 11-31"
      },
      {
        "Id": 1133,
        "Name": "Product 11-32"
      },
      {
        "Id": 1134,
        "Name": "Product 11-33"
      },
      {
        "Id": 1135,
        "Name": "Product 11-34"
      },
      {
        "Id": 1136,
        "Name": "Product 11-35"
      },
      {
        "Id": 1137,
        "Name": "Product 11-36"
      },
      {
        "Id": 1138,
        "Name": "Product 11-37"
      },
      {
        "Id": 1139,
        "Name": "Product 11-38"
      },
      {
        "Id": 1140,
        "Name": "Product 11-39"
      },
      {
        "Id": 1141,
        "Name": "Product 11-40"
      },
      {
        "Id": 1142,
        "Name": "Product 11-41"
      },
      {
        "Id": 1143,
        "Name": "Product 11-42"
      },
      {
        "Id": 1144,
        "Name": "Product 11-43"
      },
      {
        "Id": 1145,
        "Name": "Product 11-44"
      },
      {
        "Id": 1146,
        "Name": "Product 11-45"
      },
      {
        "Id": 1147,
        "Name": "Product 11-46"
      },
      {
        "Id": 1148,
        "Name": "Product 11-47"
      },
      {
        "Id": 1149,
        "Name": "Product 11-48"
      },
      {
        "Id": 1150,
        "Name": "Product 11-49"
      },
      {
        "Id": 1151,
        "Name": "Product 11-50"
      },
      {
        "Id": 1152,
        "Name": "Product 11-51"
      },
      {
        "Id": 1153,
        "Name": "Product 11-52"
      },
      {
        "Id": 1154,
        "Name": "Product 11-53"
      },
      {
        "Id": 1155,
        "Name": "Product 11-54"
      },
      {
        "Id": 1156,
        "Name": "Product 11-55"
      },
      {
        "Id": 1157,
        "Name": "Product 11-56"
      },
      {
        "Id": 1158,
        "Name": "Product 11-57"
      },
      {
        "Id": 1159,
        "Name": "Product 11-58"
      },
      {
        "Id": 1160,
        "Name": "Product 11-59"
      },
      {
        "Id": 1161,
        "Name": "Product 11-60"
      },
      {
        "Id": 1162,
        "Name": "Product 11-61"
      },
      {
        "Id": 1163,
        "Name": "Product 11-62"
      },
      {
        "Id": 1164,
        "Name": "Product 11-63"
      },
      {
        "Id": 1165,
        "Name": "Product 11-64"
      },
      {
        "Id": 1166,
        "Name": "Product 11-65"
      },
      {
        "Id": 1167,
        "Name": "Product 11-66"
      },
      {
        "Id": 1168,
        "Name": "Product 11-67"
      },
      {
        "Id": 1169,
        "Name": "Product 11-68"
      },
      {
        "Id": 1170,
        "Name": "Product 11-69"
      },
      {
        "Id": 1171,
        "Name": "Product 11-70"
      },
      {
        "Id": 1172,
        "Name": "Product 11-71"
      },
      {
        "Id": 1173,
        "Name": "Product 11-72"
      },
      {
        "Id": 1174,
        "Name": "Product 11-73"
      },
      {
        "Id": 1175,
        "Name": "Product 11-74"
      },
      {
        "Id": 1176,
        "Name": "Product 11-75"
      },
      {
        "Id": 1177,
        "Name": "Product 11-76"
      },
      {
        "Id": 1178,
        "Name": "Product 11-77"
      },
      {
        "Id": 1179,
        "Name": "Product 11-78"
      },
      {
        "Id": 1180,
        "Name": "Product 11-79"
      },
      {
        "Id": 1181,
        "Name": "Product 11-80"
      },
      {
        "Id": 1182,
        "Name": "Product 11-81"
      },
      {
        "Id": 1183,
        "Name": "Product 11-82"
      },
      {
        "Id": 1184,
        "Name": "Product 11-83"
      },
      {
        "Id": 1185,
        "Name": "Product 11-84"
      },
      {
        "Id": 1186,
        "Name": "Product 11-85"
      },
      {
        "Id": 1187,
        "Name": "Product 11-86"
      },
      {
        "Id": 1188,
        "Name": "Product 11-87"
      },
      {
        "Id": 1189,
        "Name": "Product 11-88"
      },
      {
        "Id": 1190,
        "Name": "Product 11-89"
      },
      {
        "Id": 1191,
        "Name": "Product 11-90"
      },
      {
        "Id": 1192,
        "Name": "Product 11-91"
      },
      {
        "Id": 1193,
        "Name": "Product 11-92"
      },
      {
        "Id": 1194,
        "Name": "Product 11-93"
      },
      {
        "Id": 1195,
        "Name": "Product 11-94"
      },
      {
        "Id": 1196,
        "Name": "Product 11-95"
      },
      {
        "Id": 1197,
        "Name": "Product 11-96"
      },
      {
        "Id": 1198,
        "Name": "Product 11-97"
      },
      {
        "Id": 1199,
        "Name": "Product 11-98"
      },
      {
        "Id": 1200,
        "Name": "Product 11-99"
      }
    ]
  },
  {
    "Id": 13,
    "Name": "Brand:12",
    "Products": [
      {
        "Id": 1201,
        "Name": "Product 12-0"
      },
      {
        "Id": 1202,
        "Name": "Product 12-1"
      },
      {
        "Id": 1203,
        "Name": "Product 12-2"
      },
      {
        "Id": 1204,
        "Name": "Product 12-3"
      },
      {
        "Id": 1205,
        "Name": "Product 12-4"
      },
      {
        "Id": 1206,
        "Name": "Product 12-5"
      },
      {
        "Id": 1207,
        "Name": "Product 12-6"
      },
      {
        "Id": 1208,
        "Name": "Product 12-7"
      },
      {
        "Id": 1209,
        "Name": "Product 12-8"
      },
      {
        "Id": 1210,
        "Name": "Product 12-9"
      },
      {
        "Id": 1211,
        "Name": "Product 12-10"
      },
      {
        "Id": 1212,
        "Name": "Product 12-11"
      },
      {
        "Id": 1213,
        "Name": "Product 12-12"
      },
      {
        "Id": 1214,
        "Name": "Product 12-13"
      },
      {
        "Id": 1215,
        "Name": "Product 12-14"
      },
      {
        "Id": 1216,
        "Name": "Product 12-15"
      },
      {
        "Id": 1217,
        "Name": "Product 12-16"
      },
      {
        "Id": 1218,
        "Name": "Product 12-17"
      },
      {
        "Id": 1219,
        "Name": "Product 12-18"
      },
      {
        "Id": 1220,
        "Name": "Product 12-19"
      },
      {
        "Id": 1221,
        "Name": "Product 12-20"
      },
      {
        "Id": 1222,
        "Name": "Product 12-21"
      },
      {
        "Id": 1223,
        "Name": "Product 12-22"
      },
      {
        "Id": 1224,
        "Name": "Product 12-23"
      },
      {
        "Id": 1225,
        "Name": "Product 12-24"
      },
      {
        "Id": 1226,
        "Name": "Product 12-25"
      },
      {
        "Id": 1227,
        "Name": "Product 12-26"
      },
      {
        "Id": 1228,
        "Name": "Product 12-27"
      },
      {
        "Id": 1229,
        "Name": "Product 12-28"
      },
      {
        "Id": 1230,
        "Name": "Product 12-29"
      },
      {
        "Id": 1231,
        "Name": "Product 12-30"
      },
      {
        "Id": 1232,
        "Name": "Product 12-31"
      },
      {
        "Id": 1233,
        "Name": "Product 12-32"
      },
      {
        "Id": 1234,
        "Name": "Product 12-33"
      },
      {
        "Id": 1235,
        "Name": "Product 12-34"
      },
      {
        "Id": 1236,
        "Name": "Product 12-35"
      },
      {
        "Id": 1237,
        "Name": "Product 12-36"
      },
      {
        "Id": 1238,
        "Name": "Product 12-37"
      },
      {
        "Id": 1239,
        "Name": "Product 12-38"
      },
      {
        "Id": 1240,
        "Name": "Product 12-39"
      },
      {
        "Id": 1241,
        "Name": "Product 12-40"
      },
      {
        "Id": 1242,
        "Name": "Product 12-41"
      },
      {
        "Id": 1243,
        "Name": "Product 12-42"
      },
      {
        "Id": 1244,
        "Name": "Product 12-43"
      },
      {
        "Id": 1245,
        "Name": "Product 12-44"
      },
      {
        "Id": 1246,
        "Name": "Product 12-45"
      },
      {
        "Id": 1247,
        "Name": "Product 12-46"
      },
      {
        "Id": 1248,
        "Name": "Product 12-47"
      },
      {
        "Id": 1249,
        "Name": "Product 12-48"
      },
      {
        "Id": 1250,
        "Name": "Product 12-49"
      },
      {
        "Id": 1251,
        "Name": "Product 12-50"
      },
      {
        "Id": 1252,
        "Name": "Product 12-51"
      },
      {
        "Id": 1253,
        "Name": "Product 12-52"
      },
      {
        "Id": 1254,
        "Name": "Product 12-53"
      },
      {
        "Id": 1255,
        "Name": "Product 12-54"
      },
      {
        "Id": 1256,
        "Name": "Product 12-55"
      },
      {
        "Id": 1257,
        "Name": "Product 12-56"
      },
      {
        "Id": 1258,
        "Name": "Product 12-57"
      },
      {
        "Id": 1259,
        "Name": "Product 12-58"
      },
      {
        "Id": 1260,
        "Name": "Product 12-59"
      },
      {
        "Id": 1261,
        "Name": "Product 12-60"
      },
      {
        "Id": 1262,
        "Name": "Product 12-61"
      },
      {
        "Id": 1263,
        "Name": "Product 12-62"
      },
      {
        "Id": 1264,
        "Name": "Product 12-63"
      },
      {
        "Id": 1265,
        "Name": "Product 12-64"
      },
      {
        "Id": 1266,
        "Name": "Product 12-65"
      },
      {
        "Id": 1267,
        "Name": "Product 12-66"
      },
      {
        "Id": 1268,
        "Name": "Product 12-67"
      },
      {
        "Id": 1269,
        "Name": "Product 12-68"
      },
      {
        "Id": 1270,
        "Name": "Product 12-69"
      },
      {
        "Id": 1271,
        "Name": "Product 12-70"
      },
      {
        "Id": 1272,
        "Name": "Product 12-71"
      },
      {
        "Id": 1273,
        "Name": "Product 12-72"
      },
      {
        "Id": 1274,
        "Name": "Product 12-73"
      },
      {
        "Id": 1275,
        "Name": "Product 12-74"
      },
      {
        "Id": 1276,
        "Name": "Product 12-75"
      },
      {
        "Id": 1277,
        "Name": "Product 12-76"
      },
      {
        "Id": 1278,
        "Name": "Product 12-77"
      },
      {
        "Id": 1279,
        "Name": "Product 12-78"
      },
      {
        "Id": 1280,
        "Name": "Product 12-79"
      },
      {
        "Id": 1281,
        "Name": "Product 12-80"
      },
      {
        "Id": 1282,
        "Name": "Product 12-81"
      },
      {
        "Id": 1283,
        "Name": "Product 12-82"
      },
      {
        "Id": 1284,
        "Name": "Product 12-83"
      },
      {
        "Id": 1285,
        "Name": "Product 12-84"
      },
      {
        "Id": 1286,
        "Name": "Product 12-85"
      },
      {
        "Id": 1287,
        "Name": "Product 12-86"
      },
      {
        "Id": 1288,
        "Name": "Product 12-87"
      },
      {
        "Id": 1289,
        "Name": "Product 12-88"
      },
      {
        "Id": 1290,
        "Name": "Product 12-89"
      },
      {
        "Id": 1291,
        "Name": "Product 12-90"
      },
      {
        "Id": 1292,
        "Name": "Product 12-91"
      },
      {
        "Id": 1293,
        "Name": "Product 12-92"
      },
      {
        "Id": 1294,
        "Name": "Product 12-93"
      },
      {
        "Id": 1295,
        "Name": "Product 12-94"
      },
      {
        "Id": 1296,
        "Name": "Product 12-95"
      },
      {
        "Id": 1297,
        "Name": "Product 12-96"
      },
      {
        "Id": 1298,
        "Name": "Product 12-97"
      },
      {
        "Id": 1299,
        "Name": "Product 12-98"
      },
      {
        "Id": 1300,
        "Name": "Product 12-99"
      }
    ]
  }
]
```


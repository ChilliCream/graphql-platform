# Project_Key_To_Collection_Expression_Integration

## SQL

```text
-- @__keys_0={ '2', '1' } (DbType = Object)
SELECT b."Id", p."Name", p."Id"
FROM "Brands" AS b
LEFT JOIN "Products" AS p ON b."Id" = p."BrandId"
WHERE b."Id" = ANY (@__keys_0)
ORDER BY b."Id"
```

## Result

```json
{
  "data": {
    "brandById": [
      {
        "products": [
          {
            "name": "Product 0-0"
          },
          {
            "name": "Product 0-1"
          },
          {
            "name": "Product 0-2"
          },
          {
            "name": "Product 0-3"
          },
          {
            "name": "Product 0-4"
          },
          {
            "name": "Product 0-5"
          },
          {
            "name": "Product 0-6"
          },
          {
            "name": "Product 0-7"
          },
          {
            "name": "Product 0-8"
          },
          {
            "name": "Product 0-9"
          },
          {
            "name": "Product 0-10"
          },
          {
            "name": "Product 0-11"
          },
          {
            "name": "Product 0-12"
          },
          {
            "name": "Product 0-13"
          },
          {
            "name": "Product 0-14"
          },
          {
            "name": "Product 0-15"
          },
          {
            "name": "Product 0-16"
          },
          {
            "name": "Product 0-17"
          },
          {
            "name": "Product 0-18"
          },
          {
            "name": "Product 0-19"
          },
          {
            "name": "Product 0-20"
          },
          {
            "name": "Product 0-21"
          },
          {
            "name": "Product 0-22"
          },
          {
            "name": "Product 0-23"
          },
          {
            "name": "Product 0-24"
          },
          {
            "name": "Product 0-25"
          },
          {
            "name": "Product 0-26"
          },
          {
            "name": "Product 0-27"
          },
          {
            "name": "Product 0-28"
          },
          {
            "name": "Product 0-29"
          },
          {
            "name": "Product 0-30"
          },
          {
            "name": "Product 0-31"
          },
          {
            "name": "Product 0-32"
          },
          {
            "name": "Product 0-33"
          },
          {
            "name": "Product 0-34"
          },
          {
            "name": "Product 0-35"
          },
          {
            "name": "Product 0-36"
          },
          {
            "name": "Product 0-37"
          },
          {
            "name": "Product 0-38"
          },
          {
            "name": "Product 0-39"
          },
          {
            "name": "Product 0-40"
          },
          {
            "name": "Product 0-41"
          },
          {
            "name": "Product 0-42"
          },
          {
            "name": "Product 0-43"
          },
          {
            "name": "Product 0-44"
          },
          {
            "name": "Product 0-45"
          },
          {
            "name": "Product 0-46"
          },
          {
            "name": "Product 0-47"
          },
          {
            "name": "Product 0-48"
          },
          {
            "name": "Product 0-49"
          },
          {
            "name": "Product 0-50"
          },
          {
            "name": "Product 0-51"
          },
          {
            "name": "Product 0-52"
          },
          {
            "name": "Product 0-53"
          },
          {
            "name": "Product 0-54"
          },
          {
            "name": "Product 0-55"
          },
          {
            "name": "Product 0-56"
          },
          {
            "name": "Product 0-57"
          },
          {
            "name": "Product 0-58"
          },
          {
            "name": "Product 0-59"
          },
          {
            "name": "Product 0-60"
          },
          {
            "name": "Product 0-61"
          },
          {
            "name": "Product 0-62"
          },
          {
            "name": "Product 0-63"
          },
          {
            "name": "Product 0-64"
          },
          {
            "name": "Product 0-65"
          },
          {
            "name": "Product 0-66"
          },
          {
            "name": "Product 0-67"
          },
          {
            "name": "Product 0-68"
          },
          {
            "name": "Product 0-69"
          },
          {
            "name": "Product 0-70"
          },
          {
            "name": "Product 0-71"
          },
          {
            "name": "Product 0-72"
          },
          {
            "name": "Product 0-73"
          },
          {
            "name": "Product 0-74"
          },
          {
            "name": "Product 0-75"
          },
          {
            "name": "Product 0-76"
          },
          {
            "name": "Product 0-77"
          },
          {
            "name": "Product 0-78"
          },
          {
            "name": "Product 0-79"
          },
          {
            "name": "Product 0-80"
          },
          {
            "name": "Product 0-81"
          },
          {
            "name": "Product 0-82"
          },
          {
            "name": "Product 0-83"
          },
          {
            "name": "Product 0-84"
          },
          {
            "name": "Product 0-85"
          },
          {
            "name": "Product 0-86"
          },
          {
            "name": "Product 0-87"
          },
          {
            "name": "Product 0-88"
          },
          {
            "name": "Product 0-89"
          },
          {
            "name": "Product 0-90"
          },
          {
            "name": "Product 0-91"
          },
          {
            "name": "Product 0-92"
          },
          {
            "name": "Product 0-93"
          },
          {
            "name": "Product 0-94"
          },
          {
            "name": "Product 0-95"
          },
          {
            "name": "Product 0-96"
          },
          {
            "name": "Product 0-97"
          },
          {
            "name": "Product 0-98"
          },
          {
            "name": "Product 0-99"
          }
        ]
      },
      {
        "products": [
          {
            "name": "Product 1-0"
          },
          {
            "name": "Product 1-1"
          },
          {
            "name": "Product 1-2"
          },
          {
            "name": "Product 1-3"
          },
          {
            "name": "Product 1-4"
          },
          {
            "name": "Product 1-5"
          },
          {
            "name": "Product 1-6"
          },
          {
            "name": "Product 1-7"
          },
          {
            "name": "Product 1-8"
          },
          {
            "name": "Product 1-9"
          },
          {
            "name": "Product 1-10"
          },
          {
            "name": "Product 1-11"
          },
          {
            "name": "Product 1-12"
          },
          {
            "name": "Product 1-13"
          },
          {
            "name": "Product 1-14"
          },
          {
            "name": "Product 1-15"
          },
          {
            "name": "Product 1-16"
          },
          {
            "name": "Product 1-17"
          },
          {
            "name": "Product 1-18"
          },
          {
            "name": "Product 1-19"
          },
          {
            "name": "Product 1-20"
          },
          {
            "name": "Product 1-21"
          },
          {
            "name": "Product 1-22"
          },
          {
            "name": "Product 1-23"
          },
          {
            "name": "Product 1-24"
          },
          {
            "name": "Product 1-25"
          },
          {
            "name": "Product 1-26"
          },
          {
            "name": "Product 1-27"
          },
          {
            "name": "Product 1-28"
          },
          {
            "name": "Product 1-29"
          },
          {
            "name": "Product 1-30"
          },
          {
            "name": "Product 1-31"
          },
          {
            "name": "Product 1-32"
          },
          {
            "name": "Product 1-33"
          },
          {
            "name": "Product 1-34"
          },
          {
            "name": "Product 1-35"
          },
          {
            "name": "Product 1-36"
          },
          {
            "name": "Product 1-37"
          },
          {
            "name": "Product 1-38"
          },
          {
            "name": "Product 1-39"
          },
          {
            "name": "Product 1-40"
          },
          {
            "name": "Product 1-41"
          },
          {
            "name": "Product 1-42"
          },
          {
            "name": "Product 1-43"
          },
          {
            "name": "Product 1-44"
          },
          {
            "name": "Product 1-45"
          },
          {
            "name": "Product 1-46"
          },
          {
            "name": "Product 1-47"
          },
          {
            "name": "Product 1-48"
          },
          {
            "name": "Product 1-49"
          },
          {
            "name": "Product 1-50"
          },
          {
            "name": "Product 1-51"
          },
          {
            "name": "Product 1-52"
          },
          {
            "name": "Product 1-53"
          },
          {
            "name": "Product 1-54"
          },
          {
            "name": "Product 1-55"
          },
          {
            "name": "Product 1-56"
          },
          {
            "name": "Product 1-57"
          },
          {
            "name": "Product 1-58"
          },
          {
            "name": "Product 1-59"
          },
          {
            "name": "Product 1-60"
          },
          {
            "name": "Product 1-61"
          },
          {
            "name": "Product 1-62"
          },
          {
            "name": "Product 1-63"
          },
          {
            "name": "Product 1-64"
          },
          {
            "name": "Product 1-65"
          },
          {
            "name": "Product 1-66"
          },
          {
            "name": "Product 1-67"
          },
          {
            "name": "Product 1-68"
          },
          {
            "name": "Product 1-69"
          },
          {
            "name": "Product 1-70"
          },
          {
            "name": "Product 1-71"
          },
          {
            "name": "Product 1-72"
          },
          {
            "name": "Product 1-73"
          },
          {
            "name": "Product 1-74"
          },
          {
            "name": "Product 1-75"
          },
          {
            "name": "Product 1-76"
          },
          {
            "name": "Product 1-77"
          },
          {
            "name": "Product 1-78"
          },
          {
            "name": "Product 1-79"
          },
          {
            "name": "Product 1-80"
          },
          {
            "name": "Product 1-81"
          },
          {
            "name": "Product 1-82"
          },
          {
            "name": "Product 1-83"
          },
          {
            "name": "Product 1-84"
          },
          {
            "name": "Product 1-85"
          },
          {
            "name": "Product 1-86"
          },
          {
            "name": "Product 1-87"
          },
          {
            "name": "Product 1-88"
          },
          {
            "name": "Product 1-89"
          },
          {
            "name": "Product 1-90"
          },
          {
            "name": "Product 1-91"
          },
          {
            "name": "Product 1-92"
          },
          {
            "name": "Product 1-93"
          },
          {
            "name": "Product 1-94"
          },
          {
            "name": "Product 1-95"
          },
          {
            "name": "Product 1-96"
          },
          {
            "name": "Product 1-97"
          },
          {
            "name": "Product 1-98"
          },
          {
            "name": "Product 1-99"
          }
        ]
      }
    ]
  }
}
```


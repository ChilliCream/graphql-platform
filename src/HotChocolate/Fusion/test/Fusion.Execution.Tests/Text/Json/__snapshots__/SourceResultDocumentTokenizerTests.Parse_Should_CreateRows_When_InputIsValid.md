# Parse_Should_CreateRows_When_InputIsValid

## Input 0: {}

```text
StartObject 0 0 2 False
EndObject 1 0 2 False

```

## Input 1: []

```text
StartArray 0 0 2 False
EndArray 1 0 2 False

```

## Input 2: {"a":1}

```text
StartObject 0 1 4 False
PropertyName 1 3 1 False
Number 5 1 1 False
EndObject 6 1 4 False

```

## Input 3: [1,2,3]

```text
StartArray 0 3 5 False
Number 1 1 1 False
Number 3 1 1 False
Number 5 1 1 False
EndArray 6 3 5 False

```

## Input 4: "root"

```text
String 0 6 1 False

```

## Input 5: 123

```text
Number 0 3 1 False

```

## Input 6: -0

```text
Number 0 2 1 False

```

## Input 7: 0

```text
Number 0 1 1 False

```

## Input 8: -1.5e+10

```text
Number 0 8 1 False

```

## Input 9: 1E-2

```text
Number 0 4 1 False

```

## Input 10: 0.0

```text
Number 0 3 1 False

```

## Input 11: true

```text
True 0 4 1 False

```

## Input 12: false

```text
False 0 5 1 False

```

## Input 13: null

```text
Null 0 4 1 False

```

## Input 14:  \t\r\n{ "a" : [ 1 , 2 ] , "b" : { } , "c" : [ ] } ... (49 chars)

```text
StartObject 4 3 13 True
PropertyName 6 3 1 False
StartArray 12 2 4 False
Number 14 1 1 False
Number 18 1 1 False
EndArray 20 2 4 False
PropertyName 24 3 1 False
StartObject 30 0 2 False
EndObject 32 0 2 False
PropertyName 36 3 1 False
StartArray 42 0 2 False
EndArray 44 0 2 False
EndObject 46 3 13 False

```

## Input 15: {"a":"\"\\\/\b\f\n\r\t\u00e9\uD83D\uDE00"}

```text
StartObject 0 1 4 False
PropertyName 1 3 1 False
String 5 36 1 True
EndObject 41 1 4 False

```

## Input 16: {"\u0061":1,"a\"b":2}

```text
StartObject 0 2 6 False
PropertyName 1 8 1 True
Number 10 1 1 False
PropertyName 12 6 1 True
Number 19 1 1 False
EndObject 20 2 6 False

```

## Input 17: {"a":"\uD800","b":"\uDC00x"}

```text
StartObject 0 2 6 False
PropertyName 1 3 1 False
String 5 8 1 True
PropertyName 14 3 1 False
String 18 9 1 True
EndObject 27 2 6 False

```

## Input 18: ["é","日本","😀","\u007f"]

```text
StartArray 0 4 6 False
String 1 4 1 False
String 6 8 1 False
String 15 6 1 False
String 22 3 1 False
EndArray 25 4 6 False

```

## Input 19: {"a":1}garbage

```text
StartObject 0 1 4 False
PropertyName 1 3 1 False
Number 5 1 1 False
EndObject 6 1 4 False

```

## Input 20: {"a":1}}

```text
StartObject 0 1 4 False
PropertyName 1 3 1 False
Number 5 1 1 False
EndObject 6 1 4 False

```

## Input 21: {"a":1} [

```text
StartObject 0 1 4 False
PropertyName 1 3 1 False
Number 5 1 1 False
EndObject 6 1 4 False

```

## Input 22: [[[]]]

```text
StartArray 0 1 6 True
StartArray 1 1 4 True
StartArray 2 0 2 False
EndArray 3 0 2 False
EndArray 4 1 4 False
EndArray 5 1 6 False

```

## Input 23: [{},{}]

```text
StartArray 0 2 6 True
StartObject 1 0 2 False
EndObject 2 0 2 False
StartObject 4 0 2 False
EndObject 5 0 2 False
EndArray 6 2 6 False

```

## Input 24: {"a":[{"b":null,"c":[true,false]}],"d":{"e":{}}}

```text
StartObject 0 2 20 True
PropertyName 1 3 1 False
StartArray 5 1 11 True
StartObject 6 2 9 True
PropertyName 7 3 1 False
Null 11 4 1 False
PropertyName 16 3 1 False
StartArray 20 2 4 False
True 21 4 1 False
False 26 5 1 False
EndArray 31 2 4 False
EndObject 32 2 9 False
EndArray 33 1 11 False
PropertyName 35 3 1 False
StartObject 39 1 5 True
PropertyName 40 3 1 False
StartObject 44 0 2 False
EndObject 45 0 2 False
EndObject 46 1 5 False
EndObject 47 2 20 False

```

## Input 25: [1.0e5,-0.5,123456789012345678901234567890,1e999... (57 chars)

```text
StartArray 0 5 7 False
Number 1 5 1 False
Number 7 4 1 False
Number 12 30 1 False
Number 43 5 1 False
Number 49 7 1 False
EndArray 56 5 7 False

```

## Input 26: ["xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx... (5066 chars)

```text
StartArray 0 2 4 False
String 1 5002 1 False
String 5004 61 1 True
EndArray 5065 2 4 False

```

## Input 27: {"data":{"user":null},"errors":[{"message":"x","... (93 chars)

```text
StartObject 0 2 24 True
PropertyName 1 6 1 False
StartObject 8 1 4 False
PropertyName 9 6 1 False
Null 16 4 1 False
EndObject 20 1 4 False
PropertyName 22 8 1 False
StartArray 31 1 16 True
StartObject 32 3 14 True
PropertyName 33 9 1 False
String 43 3 1 False
PropertyName 47 6 1 False
StartArray 54 2 4 False
String 55 6 1 False
Number 62 1 1 False
EndArray 63 2 4 False
PropertyName 65 12 1 False
StartObject 78 1 4 False
PropertyName 79 6 1 False
String 86 3 1 False
EndObject 89 1 4 False
EndObject 90 3 14 False
EndArray 91 1 16 False
EndObject 92 2 24 False

```

## Input 28: [1] \r\n\t 

```text
StartArray 0 1 3 False
Number 1 1 1 False
EndArray 2 1 3 False

```

## Input 29: [[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[... (128 chars)

```text
StartArray 0 1 128 True
StartArray 1 1 126 True
StartArray 2 1 124 True
StartArray 3 1 122 True
StartArray 4 1 120 True
StartArray 5 1 118 True
StartArray 6 1 116 True
StartArray 7 1 114 True
StartArray 8 1 112 True
StartArray 9 1 110 True
StartArray 10 1 108 True
StartArray 11 1 106 True
StartArray 12 1 104 True
StartArray 13 1 102 True
StartArray 14 1 100 True
StartArray 15 1 98 True
StartArray 16 1 96 True
StartArray 17 1 94 True
StartArray 18 1 92 True
StartArray 19 1 90 True
StartArray 20 1 88 True
StartArray 21 1 86 True
StartArray 22 1 84 True
StartArray 23 1 82 True
StartArray 24 1 80 True
StartArray 25 1 78 True
StartArray 26 1 76 True
StartArray 27 1 74 True
StartArray 28 1 72 True
StartArray 29 1 70 True
StartArray 30 1 68 True
StartArray 31 1 66 True
StartArray 32 1 64 True
StartArray 33 1 62 True
StartArray 34 1 60 True
StartArray 35 1 58 True
StartArray 36 1 56 True
StartArray 37 1 54 True
StartArray 38 1 52 True
StartArray 39 1 50 True
StartArray 40 1 48 True
StartArray 41 1 46 True
StartArray 42 1 44 True
StartArray 43 1 42 True
StartArray 44 1 40 True
StartArray 45 1 38 True
StartArray 46 1 36 True
StartArray 47 1 34 True
StartArray 48 1 32 True
StartArray 49 1 30 True
StartArray 50 1 28 True
StartArray 51 1 26 True
StartArray 52 1 24 True
StartArray 53 1 22 True
StartArray 54 1 20 True
StartArray 55 1 18 True
StartArray 56 1 16 True
StartArray 57 1 14 True
StartArray 58 1 12 True
StartArray 59 1 10 True
StartArray 60 1 8 True
StartArray 61 1 6 True
StartArray 62 1 4 True
StartArray 63 0 2 False
EndArray 64 0 2 False
EndArray 65 1 4 False
EndArray 66 1 6 False
EndArray 67 1 8 False
EndArray 68 1 10 False
EndArray 69 1 12 False
EndArray 70 1 14 False
EndArray 71 1 16 False
EndArray 72 1 18 False
EndArray 73 1 20 False
EndArray 74 1 22 False
EndArray 75 1 24 False
EndArray 76 1 26 False
EndArray 77 1 28 False
EndArray 78 1 30 False
EndArray 79 1 32 False
EndArray 80 1 34 False
EndArray 81 1 36 False
EndArray 82 1 38 False
EndArray 83 1 40 False
EndArray 84 1 42 False
EndArray 85 1 44 False
EndArray 86 1 46 False
EndArray 87 1 48 False
EndArray 88 1 50 False
EndArray 89 1 52 False
EndArray 90 1 54 False
EndArray 91 1 56 False
EndArray 92 1 58 False
EndArray 93 1 60 False
EndArray 94 1 62 False
EndArray 95 1 64 False
EndArray 96 1 66 False
EndArray 97 1 68 False
EndArray 98 1 70 False
EndArray 99 1 72 False
EndArray 100 1 74 False
EndArray 101 1 76 False
EndArray 102 1 78 False
EndArray 103 1 80 False
EndArray 104 1 82 False
EndArray 105 1 84 False
EndArray 106 1 86 False
EndArray 107 1 88 False
EndArray 108 1 90 False
EndArray 109 1 92 False
EndArray 110 1 94 False
EndArray 111 1 96 False
EndArray 112 1 98 False
EndArray 113 1 100 False
EndArray 114 1 102 False
EndArray 115 1 104 False
EndArray 116 1 106 False
EndArray 117 1 108 False
EndArray 118 1 110 False
EndArray 119 1 112 False
EndArray 120 1 114 False
EndArray 121 1 116 False
EndArray 122 1 118 False
EndArray 123 1 120 False
EndArray 124 1 122 False
EndArray 125 1 124 False
EndArray 126 1 126 False
EndArray 127 1 128 False

```

## Input 30: {"a":[{"a":[{"a":[{"a":[{"a":[{"a":[{"a":[{"a":[... (257 chars)

```text
StartObject 0 1 161 True
PropertyName 1 3 1 False
StartArray 5 1 158 True
StartObject 6 1 156 True
PropertyName 7 3 1 False
StartArray 11 1 153 True
StartObject 12 1 151 True
PropertyName 13 3 1 False
StartArray 17 1 148 True
StartObject 18 1 146 True
PropertyName 19 3 1 False
StartArray 23 1 143 True
StartObject 24 1 141 True
PropertyName 25 3 1 False
StartArray 29 1 138 True
StartObject 30 1 136 True
PropertyName 31 3 1 False
StartArray 35 1 133 True
StartObject 36 1 131 True
PropertyName 37 3 1 False
StartArray 41 1 128 True
StartObject 42 1 126 True
PropertyName 43 3 1 False
StartArray 47 1 123 True
StartObject 48 1 121 True
PropertyName 49 3 1 False
StartArray 53 1 118 True
StartObject 54 1 116 True
PropertyName 55 3 1 False
StartArray 59 1 113 True
StartObject 60 1 111 True
PropertyName 61 3 1 False
StartArray 65 1 108 True
StartObject 66 1 106 True
PropertyName 67 3 1 False
StartArray 71 1 103 True
StartObject 72 1 101 True
PropertyName 73 3 1 False
StartArray 77 1 98 True
StartObject 78 1 96 True
PropertyName 79 3 1 False
StartArray 83 1 93 True
StartObject 84 1 91 True
PropertyName 85 3 1 False
StartArray 89 1 88 True
StartObject 90 1 86 True
PropertyName 91 3 1 False
StartArray 95 1 83 True
StartObject 96 1 81 True
PropertyName 97 3 1 False
StartArray 101 1 78 True
StartObject 102 1 76 True
PropertyName 103 3 1 False
StartArray 107 1 73 True
StartObject 108 1 71 True
PropertyName 109 3 1 False
StartArray 113 1 68 True
StartObject 114 1 66 True
PropertyName 115 3 1 False
StartArray 119 1 63 True
StartObject 120 1 61 True
PropertyName 121 3 1 False
StartArray 125 1 58 True
StartObject 126 1 56 True
PropertyName 127 3 1 False
StartArray 131 1 53 True
StartObject 132 1 51 True
PropertyName 133 3 1 False
StartArray 137 1 48 True
StartObject 138 1 46 True
PropertyName 139 3 1 False
StartArray 143 1 43 True
StartObject 144 1 41 True
PropertyName 145 3 1 False
StartArray 149 1 38 True
StartObject 150 1 36 True
PropertyName 151 3 1 False
StartArray 155 1 33 True
StartObject 156 1 31 True
PropertyName 157 3 1 False
StartArray 161 1 28 True
StartObject 162 1 26 True
PropertyName 163 3 1 False
StartArray 167 1 23 True
StartObject 168 1 21 True
PropertyName 169 3 1 False
StartArray 173 1 18 True
StartObject 174 1 16 True
PropertyName 175 3 1 False
StartArray 179 1 13 True
StartObject 180 1 11 True
PropertyName 181 3 1 False
StartArray 185 1 8 True
StartObject 186 1 6 True
PropertyName 187 3 1 False
StartArray 191 1 3 False
Number 192 1 1 False
EndArray 193 1 3 False
EndObject 194 1 6 False
EndArray 195 1 8 False
EndObject 196 1 11 False
EndArray 197 1 13 False
EndObject 198 1 16 False
EndArray 199 1 18 False
EndObject 200 1 21 False
EndArray 201 1 23 False
EndObject 202 1 26 False
EndArray 203 1 28 False
EndObject 204 1 31 False
EndArray 205 1 33 False
EndObject 206 1 36 False
EndArray 207 1 38 False
EndObject 208 1 41 False
EndArray 209 1 43 False
EndObject 210 1 46 False
EndArray 211 1 48 False
EndObject 212 1 51 False
EndArray 213 1 53 False
EndObject 214 1 56 False
EndArray 215 1 58 False
EndObject 216 1 61 False
EndArray 217 1 63 False
EndObject 218 1 66 False
EndArray 219 1 68 False
EndObject 220 1 71 False
EndArray 221 1 73 False
EndObject 222 1 76 False
EndArray 223 1 78 False
EndObject 224 1 81 False
EndArray 225 1 83 False
EndObject 226 1 86 False
EndArray 227 1 88 False
EndObject 228 1 91 False
EndArray 229 1 93 False
EndObject 230 1 96 False
EndArray 231 1 98 False
EndObject 232 1 101 False
EndArray 233 1 103 False
EndObject 234 1 106 False
EndArray 235 1 108 False
EndObject 236 1 111 False
EndArray 237 1 113 False
EndObject 238 1 116 False
EndArray 239 1 118 False
EndObject 240 1 121 False
EndArray 241 1 123 False
EndObject 242 1 126 False
EndArray 243 1 128 False
EndObject 244 1 131 False
EndArray 245 1 133 False
EndObject 246 1 136 False
EndArray 247 1 138 False
EndObject 248 1 141 False
EndArray 249 1 143 False
EndObject 250 1 146 False
EndArray 251 1 148 False
EndObject 252 1 151 False
EndArray 253 1 153 False
EndObject 254 1 156 False
EndArray 255 1 158 False
EndObject 256 1 161 False

```

## Input 31: ["ssssssssssssssssssssssssssssssssssssssssssssss... (20004 chars)

```text
StartArray 0 1 3 False
String 1 20002 1 False
EndArray 20003 1 3 False

```

## Input 32: {"a":"\u00e9\n\u00e9\n\u00e9\n\u00e9\n\u00e9\n\u... (12008 chars)

```text
StartObject 0 1 4 False
PropertyName 1 3 1 False
String 5 12002 1 True
EndObject 12007 1 4 False

```

## Input 33: [11111111111111111111111111111111111111111111111... (6009 chars)

```text
StartArray 0 2 4 False
Number 1 3000 1 False
Number 3002 3006 1 False
EndArray 6008 2 4 False

```

## Input 34: [1E5,1e+5,1E-5,0e0,-0.0,0.5e-0,-123.456e-7]

```text
StartArray 0 7 9 False
Number 1 3 1 False
Number 5 4 1 False
Number 10 4 1 False
Number 15 3 1 False
Number 19 4 1 False
Number 24 6 1 False
Number 31 11 1 False
EndArray 42 7 9 False

```

## Input 35: [1\t,\r\n2 ]

```text
StartArray 0 2 4 False
Number 1 1 1 False
Number 6 1 1 False
EndArray 8 2 4 False

```

## Input 36: {"a":1,"a":2}

```text
StartObject 0 2 6 False
PropertyName 1 3 1 False
Number 5 1 1 False
PropertyName 7 3 1 False
Number 11 1 1 False
EndObject 12 2 6 False

```

## Input 37: {"":""}

```text
StartObject 0 1 4 False
PropertyName 1 2 1 False
String 4 2 1 False
EndObject 6 1 4 False

```

## Input 38: ["\/","\u0041\u00E9\uffff","\u004a"]

```text
StartArray 0 3 5 False
String 1 4 1 True
String 6 20 1 True
String 27 8 1 True
EndArray 35 3 5 False

```

## Input 39: {}\u0000

```text
StartObject 0 0 2 False
EndObject 1 0 2 False

```

## Input 40: "s"\n

```text
String 0 3 1 False

```

## Input 41: true\t

```text
True 0 4 1 False

```

## Input 42: -1.5 

```text
Number 0 4 1 False

```

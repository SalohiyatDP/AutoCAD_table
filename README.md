# SalohiyatTable — AutoCAD nuqta-koordinata jadvali plagini

AutoCAD chizmasida belgilangan nuqtalarni **raqamlab**, ular yoniga koordinata va
masofa **jadvalini** avtomatik yasovchi plagin. Jadval ostiga **yer maydoni**
(m.kv va gektar) hamda **chegara uzunligi** (metr) yoziladi.

Plagin ikki qismdan iborat:
- **`SalohiyatTable.dll`** — asosiy mantiq (AutoCAD .NET / ObjectARX API, C#).
- **`LoadPtable.lsp`** — DLL ni `NETLOAD` orqali yuklovchi AutoLISP fayli.

---

## Jadval ko'rinishi

| Nuqtalar T/R | Geomalumotlar |||
|:---:|:---:|:---:|:---:|
|      | **Uzunligi(m)** | **X** | **Y** |
| 1 |     | X1 | Y1 |
|   | d(1→2) |    |    |
| 2 |     | X2 | Y2 |
|   | d(2→3) |    |    |
| … |     | …  | …  |
| 1 |     | X1 | Y1 |

Jadval ostida:

```
Yer maydoni: <yuza> m.kv (<gektar> ga)
Chegara uzunligi: <perimetr> m
```

- Har bir **nuqta qatori** raqam, X va Y koordinatalarni ko'rsatadi.
- Har bir **masofa qatori** shu nuqtadan keyingi nuqtagacha bo'lgan masofani (metr) ko'rsatadi.
- Yopiq kontur uchun oxirida **1-nuqta** yana takrorlanib, kontur yopiladi.
- Koordinatalar chizma birligida (odatda **metr**) olinadi; `1 birlik = 1 metr` deb qabul qilinadi.

---

## Buyruqlar

| Buyruq | Vazifasi |
|--------|----------|
| `PTABLE`  | Nuqtalarni sichqoncha bilan ketma-ket ko'rsatib jadval yasash. |
| `PLTABLE` | Mavjud **poliliniya** (LWPOLYLINE) cho'qqilaridan jadval yasash. |
| `PTLOAD`  | DLL ni qayta yuklash (LISP funksiyasi). |

Har ikki buyruqda so'raladi:
1. **Matn balandligi** (standart `2.5`) — jadval o'lchamlari shunga moslashadi.
2. **Jadval joyi** — jadvalning yuqori-chap burchagi.

---

## Kompilyatsiya (Build)

Talablar:
- Windows + o'rnatilgan **AutoCAD** (2019+ tavsiya etiladi).
- **.NET Framework 4.8** SDK va **Visual Studio 2019/2022** yoki `dotnet` / `msbuild`.

Loyiha AutoCAD'ning boshqariladigan (managed) kutubxonalariga bog'lanadi:
`acmgd.dll`, `acdbmgd.dll`, `accoremgd.dll` — ular AutoCAD o'rnatilgan papkada bo'ladi.

`src/SalohiyatTable.csproj` faylidagi `AutoCADPath` ni o'z versiyangizga moslang
yoki build vaqtida bering:

**Visual Studio orqali (tavsiya etiladi):**
1. `SalohiyatTable.sln` faylini Visual Studio'da oching.
2. Yuqoridagi panelda konfiguratsiyani **Release**, platformani **x64** qilib tanlang.
3. **Build → Build Solution** (yoki `Ctrl+Shift+B`).

**Buyruq qatoridan (msbuild):**
```powershell
msbuild SalohiyatTable.sln /p:Configuration=Release /p:Platform=x64 ^
        /p:AutoCADPath="C:\Program Files\Autodesk\AutoCAD 2024"
```

Natijada `SalohiyatTable.dll` hosil bo'ladi (`src\bin\Release\`).

> **Eslatma:** `net48` ko'pchilik AutoCAD versiyalari (2019–2024) uchun mos.
> AutoCAD 2025+ uchun `TargetFramework` ni `net8.0-windows` ga o'zgartiring.

---

## O'rnatish va ishga tushirish

1. `SalohiyatTable.dll` va `lisp\LoadPtable.lsp` ni bitta papkaga joylang.
2. AutoCAD'da bu papkani **Options → Files → Support File Search Path** ga qo'shing.
3. `APPLOAD` buyrug'i orqali `LoadPtable.lsp` ni yuklang
   (doimiy bo'lishi uchun **Startup Suite** ga qo'shing).
4. `PTABLE` yoki `PLTABLE` buyrug'ini ishga tushiring.

Muqobil yo'l: `APPLOAD` orqali to'g'ridan-to'g'ri `SalohiyatTable.dll` ni `NETLOAD` qilish.

---

## Loyiha tuzilishi

```
AutoCAD_table/
├── SalohiyatTable.sln          # Visual Studio solution (Release|x64)
├── src/
│   ├── SalohiyatTable.csproj   # .NET loyiha fayli
│   ├── Commands.cs             # PTABLE / PLTABLE buyruqlari
│   ├── PointCollector.cs       # nuqta yig'ish (interaktiv / poliliniya)
│   ├── MarkerDrawer.cs         # nuqta raqamlarini chizish
│   ├── TableBuilder.cs         # jadval + yuza/perimetr matni
│   ├── GeometryHelper.cs       # masofa, perimetr, yuza (Shoelace)
│   └── TableOptions.cs         # o'lcham/format sozlamalari
├── lisp/
│   └── LoadPtable.lsp          # NETLOAD yuklovchisi
├── .gitignore
└── README.md
```

---

## Sozlash

Sonlar formati va o'lchamlarni `src/TableOptions.cs` faylidan o'zgartirish mumkin:
- `CoordFormat` — X/Y koordinatalar aniqligi (standart `0.###`).
- `LenFormat` — masofa/perimetr aniqligi (standart `0.##`).
- `AreaFormat`, `HaFormat` — yuza aniqligi.
- Ustun kengliklari va qator balandligi matn balandligiga proporsional.
